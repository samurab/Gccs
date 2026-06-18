#!/usr/bin/env python3
import argparse
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path


CLAUSE_PATTERN = re.compile(r"\b(?:FAR|DFARS)\s+\d{2,3}\.\d{3}-\d+\b", re.IGNORECASE)


def load_json(path):
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def detect_clauses(text):
    detections = []
    seen = set()
    for match in CLAUSE_PATTERN.finditer(text):
        citation = " ".join(match.group(0).upper().split())
        if citation in seen:
            continue
        seen.add(citation)
        line = text.count("\n", 0, match.start()) + 1
        detections.append({
            "citation": citation,
            "line": line,
            "textAnchor": match.group(0)
        })
    return detections


def evaluate_document(corpus_root, document):
    text = (corpus_root / document["file"]).read_text(encoding="utf-8")
    labels = load_json(corpus_root / document["labelFile"])
    expected = {clause["citation"].upper(): clause for clause in labels["expectedClauses"]}
    detected = {item["citation"].upper(): item for item in detect_clauses(text)}

    true_positives = sorted(set(expected).intersection(detected))
    false_positives = sorted(set(detected).difference(expected))
    false_negatives = sorted(set(expected).difference(detected))
    return {
        "documentId": document["id"],
        "title": document["title"],
        "dataClass": document["dataClass"],
        "containsCui": document["containsCui"],
        "truePositives": true_positives,
        "falsePositives": false_positives,
        "falseNegatives": false_negatives,
        "detected": [detected[key] for key in sorted(detected)],
        "expected": sorted(expected)
    }


def calculate_metrics(results):
    tp = sum(len(item["truePositives"]) for item in results)
    fp = sum(len(item["falsePositives"]) for item in results)
    fn = sum(len(item["falseNegatives"]) for item in results)
    precision = 1.0 if tp + fp == 0 else tp / (tp + fp)
    recall = 1.0 if tp + fn == 0 else tp / (tp + fn)
    return {
        "truePositiveCount": tp,
        "falsePositiveCount": fp,
        "falseNegativeCount": fn,
        "precision": round(precision, 4),
        "recall": round(recall, 4)
    }


def write_markdown(report, path):
    lines = [
        "# Extraction Evaluation",
        "",
        f"Generated at: {report['generatedAt']}",
        f"Corpus: {report['corpusPath']}",
        f"Precision: {report['metrics']['precision']}",
        f"Recall: {report['metrics']['recall']}",
        f"False positives: {report['metrics']['falsePositiveCount']}",
        f"False negatives: {report['metrics']['falseNegativeCount']}",
        f"Threshold status: {report['thresholdStatus']}",
        "",
        "## Document Results"
    ]
    for item in report["documents"]:
        lines.extend([
            "",
            f"### {item['documentId']}",
            f"- True positives: {', '.join(item['truePositives']) or 'none'}",
            f"- False positives: {', '.join(item['falsePositives']) or 'none'}",
            f"- False negatives: {', '.join(item['falseNegatives']) or 'none'}"
        ])
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description="Evaluate clause extraction precision and recall against the approved corpus.")
    parser.add_argument("--corpus", default="tests/fixtures/extraction-corpus", help="Path to extraction corpus root.")
    parser.add_argument("--output-dir", default="artifacts/extraction-evaluation", help="Directory for latest JSON and Markdown outputs.")
    parser.add_argument("--min-precision", type=float, default=0.95)
    parser.add_argument("--min-recall", type=float, default=0.95)
    args = parser.parse_args()

    corpus_root = Path(args.corpus).resolve()
    corpus = load_json(corpus_root / "corpus.json")
    allowed = set(corpus["dataHandlingRules"]["allowedDataClasses"])
    documents = corpus["documents"]
    if any(document["containsCui"] or document["dataClass"] not in allowed for document in documents):
        print("Corpus contains a disallowed or CUI document.", file=sys.stderr)
        return 2

    document_results = [evaluate_document(corpus_root, document) for document in documents]
    metrics = calculate_metrics(document_results)
    threshold_passed = metrics["precision"] >= args.min_precision and metrics["recall"] >= args.min_recall
    report = {
        "schemaVersion": "1.0",
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "corpusPath": str(corpus_root),
        "customerDataUsed": False,
        "metrics": metrics,
        "thresholds": {
            "minPrecision": args.min_precision,
            "minRecall": args.min_recall
        },
        "thresholdStatus": "passed" if threshold_passed else "failed",
        "documents": document_results
    }

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    json_path = output_dir / "latest.json"
    md_path = output_dir / "latest.md"
    json_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    write_markdown(report, md_path)

    print(f"Extraction evaluation: precision={metrics['precision']} recall={metrics['recall']} status={report['thresholdStatus']}")
    print(f"Results: {json_path}")
    if not threshold_passed:
        print("Extraction evaluation thresholds failed.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
