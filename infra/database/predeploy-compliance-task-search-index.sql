-- Run as a standalone psql script before the EF migration script.
-- CREATE INDEX CONCURRENTLY cannot execute inside an EF idempotent script's DO block.
SELECT to_regclass('gccs.compliance_tasks') IS NOT NULL AS compliance_tasks_exists \gset

\if :compliance_tasks_exists
DO $validation$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class index_relation
        JOIN pg_catalog.pg_namespace index_namespace
          ON index_namespace.oid = index_relation.relnamespace
        JOIN pg_catalog.pg_index index_state
          ON index_state.indexrelid = index_relation.oid
        WHERE index_namespace.nspname = 'gccs'
          AND index_relation.relname = 'IX_compliance_tasks_tenant_id_assigned_to_user_id_due_at_id'
          AND NOT index_state.indisvalid
    ) THEN
        RAISE EXCEPTION 'Invalid compliance task search index exists; remove it through an approved database operation before retrying.';
    END IF;
END
$validation$;

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_compliance_tasks_tenant_id_assigned_to_user_id_due_at_id"
ON gccs.compliance_tasks (tenant_id, assigned_to_user_id, due_at, id);
\else
\echo 'gccs.compliance_tasks does not exist; EF migrations will create the table and index.'
\endif
