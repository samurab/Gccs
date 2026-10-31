"""Focused package guard tests: ZIPs and paths are virtual, with no fixture writes."""
import contextlib, hashlib, io, pathlib, stat, types, unittest, warnings
from unittest.mock import patch

HERE = pathlib.Path(__file__).parent
GUARD = types.ModuleType('api_package_guard')
exec(compile((HERE / 'check-api-package.py').read_text(), 'check-api-package.py', 'exec'), GUARD.__dict__)


def archive(entries):
    stream = io.BytesIO()
    with warnings.catch_warnings():
        warnings.simplefilter('ignore', UserWarning)
        with GUARD.zipfile.ZipFile(stream, 'w') as output:
            for name, value, mode in entries:
                item = GUARD.zipfile.ZipInfo(name)
                item.create_system = 3
                item.external_attr = mode << 16
                output.writestr(item, value)
    return stream.getvalue()


API = ('Gccs.Api.dll', b'api', stat.S_IFREG | 0o644)


class VirtualPath:
    """Finite read-only path interface; mutation methods intentionally absent."""
    tree = {}
    blobs = {}
    reads = {}

    def __new__(cls, name):
        return name if isinstance(name, cls) else super().__new__(cls)

    def __init__(self, name):
        if not isinstance(name, VirtualPath): self.name = str(name)

    def is_symlink(self): return self.tree.get(self.name, (0, b''))[0] == stat.S_IFLNK
    def is_dir(self): return self.name == 'root' and not self.is_symlink()
    def is_file(self): return self.name in self.blobs
    def stat(self): return types.SimpleNamespace(st_size=len(self.blobs[self.name][0]))
    def read_bytes(self):
        choices = self.blobs[self.name]
        index = self.reads.get(self.name, 0)
        self.reads[self.name] = index + 1
        return choices[min(index, len(choices)-1)]
    def rglob(self, pattern):
        assert pattern == '*'
        return [VirtualPath('root/' + name) for name in self.tree if name != 'root']
    def lstat(self): return types.SimpleNamespace(st_mode=self.tree[self.name.removeprefix('root/')][0])
    def relative_to(self, root): return types.SimpleNamespace(as_posix=lambda: self.name.removeprefix('root/'))
    def open(self, mode):
        assert mode == 'rb'
        return io.BytesIO(self.tree[self.name.removeprefix('root/')][1])


class PackageTests(unittest.TestCase):
    def setUp(self):
        VirtualPath.tree = {'Gccs.Api.dll': (stat.S_IFREG | 0o644, b'api')}
        VirtualPath.blobs = {}
        VirtualPath.reads = {}

    def invoke(self, callback):
        with patch.object(GUARD.pathlib, 'Path', VirtualPath): return callback()

    def test_nested_dotfile_executable_and_empty_directory(self):
        result = GUARD.validate_archive(archive([API, ('nested/.config', b'x', stat.S_IFREG | 0o644), ('run', b'x', stat.S_IFREG | 0o755), ('empty/', b'', stat.S_IFDIR | 0o755)]))
        self.assertEqual(result['run']['executable'], 0o111)
        self.assertIn('nested/.config', result)
        self.assertNotIn('empty', result)

    def test_unsafe_paths_and_root_collision(self):
        for name in ['/absolute', '../outside', 'a/../b', 'a//b', 'a\\b', 'a:b', './bad', 'release.zip']:
            with self.subTest(name=name), self.assertRaises(ValueError):
                GUARD.validate_archive(archive([API, (name,b'x',stat.S_IFREG|0o644)]))

    def test_wrapped_and_missing_api(self):
        with self.assertRaises(ValueError): GUARD.validate_archive(archive([('wrapper/Gccs.Api.dll',b'api',stat.S_IFREG|0o644)]))

    def test_links_special_and_inconsistent_kind(self):
        for mode,name in [(stat.S_IFLNK|0o777,'link'),(stat.S_IFIFO|0o600,'fifo'),(stat.S_IFDIR|0o755,'not-directory'),(stat.S_IFREG|0o644,'directory/')]:
            with self.subTest(mode=mode), self.assertRaises(ValueError): GUARD.validate_archive(archive([API,(name,b'x',mode)]))

    def test_duplicate_and_file_ancestor_in_both_orders(self):
        cases=[[API,API],[API,('a',b'x',stat.S_IFREG|0o644),('a/b',b'x',stat.S_IFREG|0o644)],[API,('a/b',b'x',stat.S_IFREG|0o644),('a',b'x',stat.S_IFREG|0o644)]]
        for rows in cases:
            with self.subTest(rows=rows),self.assertRaises(ValueError):GUARD.validate_archive(archive(rows))

    def test_archive_and_expanded_bounds(self):
        blob=archive([API])
        with patch.object(GUARD,'MAX_ARCHIVE',len(blob)-1),self.assertRaises(ValueError):GUARD.validate_archive(blob)
        with patch.object(GUARD,'MAX_EXPANDED',2),self.assertRaises(ValueError):GUARD.validate_archive(blob)

    def test_encrypted_and_malformed(self):
        blob=bytearray(archive([API])); location=blob.index(b'PK\x01\x02');blob[location+8]|=1
        with self.assertRaises(ValueError):GUARD.validate_archive(blob)
        with self.assertRaises(GUARD.zipfile.BadZipFile):GUARD.validate_archive(b'not zip')

    def test_publish_tree_parity_and_original_digest(self):
        blob=archive([API]);VirtualPath.blobs={'zip':[blob]};digest=hashlib.sha256(blob).hexdigest()
        self.assertEqual(self.invoke(lambda:GUARD.inspect_paths('zip','root',digest)),GUARD.validate_archive(blob))
        with self.assertRaises(ValueError): self.invoke(lambda:GUARD.inspect_paths('zip','root','0'*64))
        VirtualPath.tree['Gccs.Api.dll']=(stat.S_IFREG|0o755,b'api')
        with self.assertRaises(ValueError): self.invoke(lambda:GUARD.inspect_paths('zip','root',digest))

    def test_root_collision_dangling_nested_link_and_special_before_zip(self):
        for name,mode in [('release.zip',stat.S_IFREG),('release.zip',stat.S_IFLNK),('nested/link',stat.S_IFLNK),('pipe',stat.S_IFIFO)]:
            VirtualPath.tree={'Gccs.Api.dll':(stat.S_IFREG|0o644,b'api'),name:(mode,b'')}
            with self.subTest(name=name,mode=mode),self.assertRaises(ValueError):self.invoke(lambda:GUARD.inspect_tree('root'))

    def test_archive_changes_during_read_or_tree_comparison(self):
        blob=archive([API]);other=archive([('Gccs.Api.dll',b'other',stat.S_IFREG|0o644)]);digest=hashlib.sha256(blob).hexdigest()
        for reads in [[blob,other],[blob,blob,other]]:
            VirtualPath.blobs={'zip':reads};VirtualPath.reads={}
            with self.subTest(reads=len(reads)),self.assertRaises(ValueError):self.invoke(lambda:GUARD.inspect_paths('zip','root',digest))

    def test_ambiguous_names_and_unicode_extra_fields(self):
        for name in ['control\x01', 'line\nname', 'delete\x7f', 'cafe\u0301']:
            with self.subTest(name=name), self.assertRaises(ValueError):
                GUARD.validate_archive(archive([API,(name,b'x',stat.S_IFREG|0o644)]))
        blob=archive([API,('abcde',b'x',stat.S_IFREG|0o644)]).replace(b'abcde',b'a\x00cde')
        with self.assertRaises(ValueError):GUARD.validate_archive(blob)
        for extra in [b'\x75\x70\x00\x00', b'\x01', b'\x01\x00\x05\x00x']:
            stream=io.BytesIO()
            with GUARD.zipfile.ZipFile(stream,'w') as output:
                output.writestr('Gccs.Api.dll',b'api')
                item=GUARD.zipfile.ZipInfo('entry');item.extra=extra
                output.writestr(item,b'x')
            with self.subTest(extra=extra),self.assertRaises((ValueError,GUARD.zipfile.BadZipFile)):
                GUARD.validate_archive(stream.getvalue())

    def test_cli_fails_closed_and_has_separate_tree_mode(self):
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(GUARD.main([]),1)
            self.assertEqual(GUARD.main(['zip','BAD']),1)
            self.assertEqual(GUARD.main(['zip','0'*64,'--check-tree','root']),1)
            self.assertEqual(self.invoke(lambda:GUARD.main(['--check-tree','root'])),0)

if __name__ == '__main__': unittest.main(verbosity=2)
