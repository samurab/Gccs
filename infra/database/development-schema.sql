DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'gccs') THEN
        CREATE SCHEMA gccs;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS gccs."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'gccs') THEN
            CREATE SCHEMA gccs;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.clauses (
        id text NOT NULL,
        source text NOT NULL,
        number text NOT NULL,
        title text NOT NULL,
        plain_english_summary text NOT NULL,
        applicability_logic text NOT NULL,
        required_action_ids_json jsonb NOT NULL,
        usually_requires_flow_down boolean NOT NULL,
        source_name text NOT NULL,
        source_url text NOT NULL,
        source_last_reviewed_at date NOT NULL,
        source_effective_at date,
        source_confidence text NOT NULL,
        source_requires_expert_review boolean NOT NULL,
        last_reviewed_at date NOT NULL,
        reviewed_by_user_id uuid,
        next_review_due_at date,
        confidence text NOT NULL,
        requires_expert_review boolean NOT NULL,
        CONSTRAINT "PK_clauses" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.controls (
        id text NOT NULL,
        framework character varying(64) NOT NULL,
        cmmc_level character varying(64) NOT NULL,
        family text NOT NULL,
        title text NOT NULL,
        requirement text NOT NULL,
        assessment_objective text NOT NULL,
        evidence_examples_json jsonb NOT NULL,
        source_name text NOT NULL,
        source_url text NOT NULL,
        source_last_reviewed_at date NOT NULL,
        source_effective_at date,
        source_confidence text NOT NULL,
        source_requires_expert_review boolean NOT NULL,
        CONSTRAINT "PK_controls" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.mvp_modules (
        key text NOT NULL,
        name text NOT NULL,
        purpose text NOT NULL,
        status text NOT NULL,
        CONSTRAINT "PK_mvp_modules" PRIMARY KEY (key)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.obligations (
        id text NOT NULL,
        source text NOT NULL,
        title text NOT NULL,
        plain_english_summary text NOT NULL,
        trigger_condition text NOT NULL,
        required_action text NOT NULL,
        owner_function text NOT NULL,
        risk_level character varying(64) NOT NULL,
        flow_down_requirement text NOT NULL,
        applicability_json jsonb NOT NULL,
        evidence_examples_json jsonb NOT NULL,
        source_name text NOT NULL,
        source_url text NOT NULL,
        source_last_reviewed_at date NOT NULL,
        source_effective_at date,
        source_confidence text NOT NULL,
        source_requires_expert_review boolean NOT NULL,
        CONSTRAINT "PK_obligations" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.tenants (
        id uuid NOT NULL,
        name character varying(240) NOT NULL,
        status character varying(64) NOT NULL,
        data_posture character varying(64) NOT NULL,
        trial_ends_at date,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_tenants" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.annual_affirmations (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        level character varying(64) NOT NULL,
        due_at date NOT NULL,
        submitted_at date,
        submitted_by_user_id uuid,
        confirmation_reference text,
        status character varying(64) NOT NULL,
        CONSTRAINT "PK_annual_affirmations" PRIMARY KEY (id),
        CONSTRAINT "FK_annual_affirmations_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.assessments (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        type character varying(64) NOT NULL,
        level character varying(64) NOT NULL,
        status character varying(64) NOT NULL,
        started_at date NOT NULL,
        completed_at date,
        affirmation_due_at date,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_assessments" PRIMARY KEY (id),
        CONSTRAINT "FK_assessments_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.assets (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name text NOT NULL,
        type character varying(64) NOT NULL,
        description text NOT NULL,
        owner_function text NOT NULL,
        stores_fci boolean NOT NULL,
        stores_cui boolean NOT NULL,
        system_boundary_id uuid,
        tags_json jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_assets" PRIMARY KEY (id),
        CONSTRAINT "FK_assets_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.audit_log_entries (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        actor_user_id uuid,
        action character varying(64) NOT NULL,
        entity_type text NOT NULL,
        entity_id text NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        ip_address text NOT NULL,
        user_agent text NOT NULL,
        summary text NOT NULL,
        metadata_json jsonb NOT NULL,
        CONSTRAINT "PK_audit_log_entries" PRIMARY KEY (id),
        CONSTRAINT "FK_audit_log_entries_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.company_profiles (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        legal_entity_name character varying(240) NOT NULL,
        doing_business_as text,
        uei character varying(32),
        cage_code character varying(16),
        sam_registration_expires_at date,
        contractor_role character varying(64) NOT NULL,
        products_and_services text NOT NULL,
        employee_range character varying(64) NOT NULL,
        revenue_range character varying(64) NOT NULL,
        it_environment_description text NOT NULL,
        uses_external_service_provider boolean NOT NULL,
        external_service_provider_name text,
        key_systems_json jsonb NOT NULL,
        agency_customers_json jsonb NOT NULL,
        data_handling_posture character varying(64) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_company_profiles" PRIMARY KEY (id),
        CONSTRAINT "FK_company_profiles_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.compliance_tasks (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        title character varying(240) NOT NULL,
        description text NOT NULL,
        type character varying(64) NOT NULL,
        status character varying(64) NOT NULL,
        risk_level character varying(64) NOT NULL,
        assigned_to_user_id uuid,
        owner_function character varying(120) NOT NULL,
        due_at date,
        contract_id uuid,
        obligation_id text,
        control_id text,
        evidence_item_id uuid,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_compliance_tasks" PRIMARY KEY (id),
        CONSTRAINT "FK_compliance_tasks_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.contracts (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        contract_number character varying(120) NOT NULL,
        title character varying(300) NOT NULL,
        agency_or_prime_name text NOT NULL,
        relationship character varying(64) NOT NULL,
        kind character varying(64) NOT NULL,
        status character varying(64) NOT NULL,
        awarded_at date,
        period_of_performance_start date NOT NULL,
        period_of_performance_end date NOT NULL,
        place_of_performance text NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_contracts" PRIMARY KEY (id),
        CONSTRAINT "FK_contracts_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.employees (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        employee_number text NOT NULL,
        name text NOT NULL,
        email text NOT NULL,
        status character varying(64) NOT NULL,
        job_title text NOT NULL,
        labor_category text NOT NULL,
        handles_fci boolean NOT NULL,
        handles_cui boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_employees" PRIMARY KEY (id),
        CONSTRAINT "FK_employees_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.evidence_items (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name character varying(240) NOT NULL,
        description text NOT NULL,
        type character varying(64) NOT NULL,
        status character varying(64) NOT NULL,
        storage_uri text,
        file_hash text,
        effective_at date,
        expires_at date,
        tags_json jsonb NOT NULL,
        approved_by_user_id uuid,
        approved_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_evidence_items" PRIMARY KEY (id),
        CONSTRAINT "FK_evidence_items_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.labor_classifications (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        employee_id uuid NOT NULL,
        contract_id uuid NOT NULL,
        labor_category text NOT NULL,
        basis_for_classification text NOT NULL,
        wage_determination_id uuid,
        evidence_item_id uuid,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_labor_classifications" PRIMARY KEY (id),
        CONSTRAINT "FK_labor_classifications_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.payroll_records (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        employee_id uuid NOT NULL,
        contract_id uuid NOT NULL,
        period_start date NOT NULL,
        period_end date NOT NULL,
        hours_worked numeric(10,2) NOT NULL,
        wage_paid numeric(12,2) NOT NULL,
        fringe_paid numeric(12,2) NOT NULL,
        evidence_item_id uuid,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_payroll_records" PRIMARY KEY (id),
        CONSTRAINT "FK_payroll_records_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.poam_items (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        control_id text NOT NULL,
        weakness text NOT NULL,
        planned_remediation text NOT NULL,
        risk_level character varying(64) NOT NULL,
        status character varying(64) NOT NULL,
        owner_user_id uuid,
        target_completion_at date NOT NULL,
        completed_at date,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_poam_items" PRIMARY KEY (id),
        CONSTRAINT "FK_poam_items_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.reports (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        type character varying(64) NOT NULL,
        title text NOT NULL,
        status character varying(64) NOT NULL,
        generated_at timestamp with time zone NOT NULL,
        generated_by_user_id uuid NOT NULL,
        storage_uri text,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_reports" PRIMARY KEY (id),
        CONSTRAINT "FK_reports_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.roles (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name character varying(120) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_roles" PRIMARY KEY (id),
        CONSTRAINT "FK_roles_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.solicitations (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        solicitation_number text NOT NULL,
        title text NOT NULL,
        agency text NOT NULL,
        response_due_at date,
        expected_contract_kind character varying(64) NOT NULL,
        set_aside text NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_solicitations" PRIMARY KEY (id),
        CONSTRAINT "FK_solicitations_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.subcontractors (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name text NOT NULL,
        uei text,
        cage_code text,
        status character varying(64) NOT NULL,
        workshare_description text NOT NULL,
        workshare_percentage numeric(5,2),
        has_fci_access boolean NOT NULL,
        has_cui_access boolean NOT NULL,
        required_cmmc_level text,
        contact_name text,
        contact_email text,
        contact_phone text,
        contact_title text,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_subcontractors" PRIMARY KEY (id),
        CONSTRAINT "FK_subcontractors_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.system_boundaries (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name text NOT NULL,
        description text NOT NULL,
        status character varying(64) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_system_boundaries" PRIMARY KEY (id),
        CONSTRAINT "FK_system_boundaries_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.training_records (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        employee_id uuid NOT NULL,
        training_name text NOT NULL,
        type character varying(64) NOT NULL,
        status character varying(64) NOT NULL,
        assigned_at date NOT NULL,
        completed_at date,
        expires_at date,
        evidence_item_id uuid,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_training_records" PRIMARY KEY (id),
        CONSTRAINT "FK_training_records_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.users (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        email character varying(320) NOT NULL,
        display_name character varying(200) NOT NULL,
        status character varying(64) NOT NULL,
        mfa_enabled boolean NOT NULL,
        last_signed_in_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_users" PRIMARY KEY (id),
        CONSTRAINT "FK_users_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.vendors (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name character varying(240) NOT NULL,
        type character varying(64) NOT NULL,
        risk_level character varying(64) NOT NULL,
        contact_name text,
        contact_email text,
        contact_phone text,
        contact_title text,
        has_fci_access boolean NOT NULL,
        has_cui_access boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_vendors" PRIMARY KEY (id),
        CONSTRAINT "FK_vendors_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.wage_determinations (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        determination_number text NOT NULL,
        revision text NOT NULL,
        place_of_performance text NOT NULL,
        effective_at date NOT NULL,
        expires_at date,
        source_url text,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_wage_determinations" PRIMARY KEY (id),
        CONSTRAINT "FK_wage_determinations_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.control_assessments (
        assessment_id uuid NOT NULL,
        control_id text NOT NULL,
        implementation_status character varying(64) NOT NULL,
        result character varying(64) NOT NULL,
        notes text NOT NULL,
        assessed_by_user_id uuid,
        assessed_at date,
        evidence_item_ids_json jsonb NOT NULL,
        CONSTRAINT "PK_control_assessments" PRIMARY KEY (assessment_id, control_id),
        CONSTRAINT "FK_control_assessments_assessments_assessment_id" FOREIGN KEY (assessment_id) REFERENCES gccs.assessments (id) ON DELETE CASCADE,
        CONSTRAINT "FK_control_assessments_controls_control_id" FOREIGN KEY (control_id) REFERENCES gccs.controls (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.company_certifications (
        id uuid NOT NULL,
        company_profile_id uuid NOT NULL,
        type character varying(64) NOT NULL,
        status character varying(64) NOT NULL,
        issuer character varying(180) NOT NULL,
        effective_at date,
        expires_at date,
        reference_number character varying(120),
        CONSTRAINT "PK_company_certifications" PRIMARY KEY (id),
        CONSTRAINT "FK_company_certifications_company_profiles_company_profile_id" FOREIGN KEY (company_profile_id) REFERENCES gccs.company_profiles (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.company_locations (
        id uuid NOT NULL,
        company_profile_id uuid NOT NULL,
        name text NOT NULL,
        street1 text NOT NULL,
        street2 text,
        city text NOT NULL,
        state_or_province text NOT NULL,
        postal_code text NOT NULL,
        country text NOT NULL,
        is_place_of_performance boolean NOT NULL,
        CONSTRAINT "PK_company_locations" PRIMARY KEY (id),
        CONSTRAINT "FK_company_locations_company_profiles_company_profile_id" FOREIGN KEY (company_profile_id) REFERENCES gccs.company_profiles (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.company_naics_codes (
        id uuid NOT NULL,
        company_profile_id uuid NOT NULL,
        code character varying(12) NOT NULL,
        title character varying(240) NOT NULL,
        is_primary boolean NOT NULL,
        size_standard text,
        qualifies_as_small boolean,
        last_checked_at date,
        CONSTRAINT "PK_company_naics_codes" PRIMARY KEY (id),
        CONSTRAINT "FK_company_naics_codes_company_profiles_company_profile_id" FOREIGN KEY (company_profile_id) REFERENCES gccs.company_profiles (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.contract_clauses (
        id uuid NOT NULL,
        contract_id uuid NOT NULL,
        clause_number text NOT NULL,
        title text NOT NULL,
        alternate text,
        full_text text,
        source character varying(64) NOT NULL,
        requires_flow_down boolean NOT NULL,
        last_reviewed_at date NOT NULL,
        reviewed_by_user_id uuid,
        next_review_due_at date,
        confidence text NOT NULL,
        requires_expert_review boolean NOT NULL,
        CONSTRAINT "PK_contract_clauses" PRIMARY KEY (id),
        CONSTRAINT "FK_contract_clauses_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.contract_deliverables (
        id uuid NOT NULL,
        contract_id uuid NOT NULL,
        name text NOT NULL,
        description text NOT NULL,
        due_at date,
        owner_function text NOT NULL,
        status character varying(64) NOT NULL,
        CONSTRAINT "PK_contract_deliverables" PRIMARY KEY (id),
        CONSTRAINT "FK_contract_deliverables_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.contract_documents (
        id uuid NOT NULL,
        contract_id uuid NOT NULL,
        type character varying(64) NOT NULL,
        file_name character varying(300) NOT NULL,
        storage_uri text,
        extracted_text_hash text,
        uploaded_at timestamp with time zone NOT NULL,
        uploaded_by_user_id uuid NOT NULL,
        contains_potential_cui boolean NOT NULL,
        CONSTRAINT "PK_contract_documents" PRIMARY KEY (id),
        CONSTRAINT "FK_contract_documents_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.contract_reporting_deadlines (
        id uuid NOT NULL,
        contract_id uuid NOT NULL,
        name text NOT NULL,
        description text NOT NULL,
        due_at date NOT NULL,
        recurrence character varying(64) NOT NULL,
        owner_function text NOT NULL,
        source_clause_numbers_json jsonb NOT NULL,
        CONSTRAINT "PK_contract_reporting_deadlines" PRIMARY KEY (id),
        CONSTRAINT "FK_contract_reporting_deadlines_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.evidence_contracts (
        evidence_item_id uuid NOT NULL,
        contract_id uuid NOT NULL,
        CONSTRAINT "PK_evidence_contracts" PRIMARY KEY (evidence_item_id, contract_id),
        CONSTRAINT "FK_evidence_contracts_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE,
        CONSTRAINT "FK_evidence_contracts_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.evidence_controls (
        evidence_item_id uuid NOT NULL,
        control_id text NOT NULL,
        CONSTRAINT "PK_evidence_controls" PRIMARY KEY (evidence_item_id, control_id),
        CONSTRAINT "FK_evidence_controls_controls_control_id" FOREIGN KEY (control_id) REFERENCES gccs.controls (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_evidence_controls_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.evidence_employees (
        evidence_item_id uuid NOT NULL,
        employee_id uuid NOT NULL,
        CONSTRAINT "PK_evidence_employees" PRIMARY KEY (evidence_item_id, employee_id),
        CONSTRAINT "FK_evidence_employees_employees_employee_id" FOREIGN KEY (employee_id) REFERENCES gccs.employees (id) ON DELETE CASCADE,
        CONSTRAINT "FK_evidence_employees_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.evidence_obligations (
        evidence_item_id uuid NOT NULL,
        obligation_id text NOT NULL,
        CONSTRAINT "PK_evidence_obligations" PRIMARY KEY (evidence_item_id, obligation_id),
        CONSTRAINT "FK_evidence_obligations_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
        CONSTRAINT "FK_evidence_obligations_obligations_obligation_id" FOREIGN KEY (obligation_id) REFERENCES gccs.obligations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.poam_evidence (
        poam_item_id uuid NOT NULL,
        evidence_item_id uuid NOT NULL,
        CONSTRAINT "PK_poam_evidence" PRIMARY KEY (poam_item_id, evidence_item_id),
        CONSTRAINT "FK_poam_evidence_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
        CONSTRAINT "FK_poam_evidence_poam_items_poam_item_id" FOREIGN KEY (poam_item_id) REFERENCES gccs.poam_items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.report_contracts (
        report_id uuid NOT NULL,
        contract_id uuid NOT NULL,
        report_entity_id uuid,
        CONSTRAINT "PK_report_contracts" PRIMARY KEY (report_id, contract_id),
        CONSTRAINT "FK_report_contracts_reports_report_entity_id" FOREIGN KEY (report_entity_id) REFERENCES gccs.reports (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.report_evidence (
        report_id uuid NOT NULL,
        evidence_item_id uuid NOT NULL,
        report_entity_id uuid,
        CONSTRAINT "PK_report_evidence" PRIMARY KEY (report_id, evidence_item_id),
        CONSTRAINT "FK_report_evidence_reports_report_entity_id" FOREIGN KEY (report_entity_id) REFERENCES gccs.reports (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.report_obligations (
        report_id uuid NOT NULL,
        obligation_id text NOT NULL,
        report_entity_id uuid,
        CONSTRAINT "PK_report_obligations" PRIMARY KEY (report_id, obligation_id),
        CONSTRAINT "FK_report_obligations_reports_report_entity_id" FOREIGN KEY (report_entity_id) REFERENCES gccs.reports (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.role_permissions (
        role_id uuid NOT NULL,
        permission character varying(64) NOT NULL,
        CONSTRAINT "PK_role_permissions" PRIMARY KEY (role_id, permission),
        CONSTRAINT "FK_role_permissions_roles_role_id" FOREIGN KEY (role_id) REFERENCES gccs.roles (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.contract_subcontractors (
        contract_id uuid NOT NULL,
        subcontractor_id uuid NOT NULL,
        CONSTRAINT "PK_contract_subcontractors" PRIMARY KEY (contract_id, subcontractor_id),
        CONSTRAINT "FK_contract_subcontractors_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE,
        CONSTRAINT "FK_contract_subcontractors_subcontractors_subcontractor_id" FOREIGN KEY (subcontractor_id) REFERENCES gccs.subcontractors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.flow_down_clauses (
        id uuid NOT NULL,
        subcontractor_id uuid NOT NULL,
        clause_number text NOT NULL,
        title text NOT NULL,
        status character varying(64) NOT NULL,
        sent_at date,
        signed_at date,
        signed_evidence_item_id uuid,
        CONSTRAINT "PK_flow_down_clauses" PRIMARY KEY (id),
        CONSTRAINT "FK_flow_down_clauses_subcontractors_subcontractor_id" FOREIGN KEY (subcontractor_id) REFERENCES gccs.subcontractors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.subcontractor_evidence (
        subcontractor_id uuid NOT NULL,
        evidence_item_id uuid NOT NULL,
        CONSTRAINT "PK_subcontractor_evidence" PRIMARY KEY (subcontractor_id, evidence_item_id),
        CONSTRAINT "FK_subcontractor_evidence_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
        CONSTRAINT "FK_subcontractor_evidence_subcontractors_subcontractor_id" FOREIGN KEY (subcontractor_id) REFERENCES gccs.subcontractors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.system_boundary_assets (
        system_boundary_id uuid NOT NULL,
        asset_id uuid NOT NULL,
        CONSTRAINT "PK_system_boundary_assets" PRIMARY KEY (system_boundary_id, asset_id),
        CONSTRAINT "FK_system_boundary_assets_assets_asset_id" FOREIGN KEY (asset_id) REFERENCES gccs.assets (id) ON DELETE CASCADE,
        CONSTRAINT "FK_system_boundary_assets_system_boundaries_system_boundary_id" FOREIGN KEY (system_boundary_id) REFERENCES gccs.system_boundaries (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.system_boundary_evidence (
        system_boundary_id uuid NOT NULL,
        evidence_item_id uuid NOT NULL,
        CONSTRAINT "PK_system_boundary_evidence" PRIMARY KEY (system_boundary_id, evidence_item_id),
        CONSTRAINT "FK_system_boundary_evidence_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
        CONSTRAINT "FK_system_boundary_evidence_system_boundaries_system_boundary_~" FOREIGN KEY (system_boundary_id) REFERENCES gccs.system_boundaries (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.user_roles (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        CONSTRAINT "PK_user_roles" PRIMARY KEY (user_id, role_id),
        CONSTRAINT "FK_user_roles_roles_role_id" FOREIGN KEY (role_id) REFERENCES gccs.roles (id) ON DELETE CASCADE,
        CONSTRAINT "FK_user_roles_users_user_id" FOREIGN KEY (user_id) REFERENCES gccs.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.evidence_vendors (
        evidence_item_id uuid NOT NULL,
        vendor_id uuid NOT NULL,
        CONSTRAINT "PK_evidence_vendors" PRIMARY KEY (evidence_item_id, vendor_id),
        CONSTRAINT "FK_evidence_vendors_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
        CONSTRAINT "FK_evidence_vendors_vendors_vendor_id" FOREIGN KEY (vendor_id) REFERENCES gccs.vendors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.system_boundary_external_service_providers (
        system_boundary_id uuid NOT NULL,
        vendor_id uuid NOT NULL,
        CONSTRAINT "PK_system_boundary_external_service_providers" PRIMARY KEY (system_boundary_id, vendor_id),
        CONSTRAINT "FK_system_boundary_external_service_providers_system_boundarie~" FOREIGN KEY (system_boundary_id) REFERENCES gccs.system_boundaries (id) ON DELETE CASCADE,
        CONSTRAINT "FK_system_boundary_external_service_providers_vendors_vendor_id" FOREIGN KEY (vendor_id) REFERENCES gccs.vendors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.labor_category_rates (
        id uuid NOT NULL,
        wage_determination_id uuid NOT NULL,
        labor_category text NOT NULL,
        hourly_wage numeric(12,2) NOT NULL,
        fringe_benefit_rate numeric(12,2) NOT NULL,
        currency text NOT NULL,
        CONSTRAINT "PK_labor_category_rates" PRIMARY KEY (id),
        CONSTRAINT "FK_labor_category_rates_wage_determinations_wage_determination~" FOREIGN KEY (wage_determination_id) REFERENCES gccs.wage_determinations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE TABLE gccs.contract_clause_obligations (
        contract_clause_id uuid NOT NULL,
        obligation_id text NOT NULL,
        CONSTRAINT "PK_contract_clause_obligations" PRIMARY KEY (contract_clause_id, obligation_id),
        CONSTRAINT "FK_contract_clause_obligations_contract_clauses_contract_claus~" FOREIGN KEY (contract_clause_id) REFERENCES gccs.contract_clauses (id) ON DELETE CASCADE,
        CONSTRAINT "FK_contract_clause_obligations_obligations_obligation_id" FOREIGN KEY (obligation_id) REFERENCES gccs.obligations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_annual_affirmations_tenant_id_status_due_at" ON gccs.annual_affirmations (tenant_id, status, due_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_assessments_created_at_updated_at" ON gccs.assessments (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_assessments_tenant_id_status_level" ON gccs.assessments (tenant_id, status, level);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_assets_created_at_updated_at" ON gccs.assets (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_assets_tenant_id_system_boundary_id" ON gccs.assets (tenant_id, system_boundary_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_audit_log_entries_tenant_id_entity_type_entity_id" ON gccs.audit_log_entries (tenant_id, entity_type, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_audit_log_entries_tenant_id_occurred_at" ON gccs.audit_log_entries (tenant_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_clauses_source_number" ON gccs.clauses (source, number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_company_certifications_company_profile_id_type" ON gccs.company_certifications (company_profile_id, type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_company_locations_company_profile_id" ON gccs.company_locations (company_profile_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_company_naics_codes_company_profile_id_code" ON gccs.company_naics_codes (company_profile_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_company_profiles_created_at_updated_at" ON gccs.company_profiles (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_company_profiles_tenant_id" ON gccs.company_profiles (tenant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_company_profiles_uei" ON gccs.company_profiles (uei);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_compliance_tasks_created_at_updated_at" ON gccs.compliance_tasks (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_compliance_tasks_tenant_id_contract_id" ON gccs.compliance_tasks (tenant_id, contract_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_compliance_tasks_tenant_id_obligation_id" ON gccs.compliance_tasks (tenant_id, obligation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_compliance_tasks_tenant_id_status_due_at" ON gccs.compliance_tasks (tenant_id, status, due_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_contract_clause_obligations_obligation_id" ON gccs.contract_clause_obligations (obligation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_contract_clauses_contract_id_clause_number" ON gccs.contract_clauses (contract_id, clause_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_contract_deliverables_contract_id_due_at" ON gccs.contract_deliverables (contract_id, due_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_contract_documents_contract_id_type" ON gccs.contract_documents (contract_id, type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_contract_reporting_deadlines_contract_id_due_at" ON gccs.contract_reporting_deadlines (contract_id, due_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_contract_subcontractors_subcontractor_id" ON gccs.contract_subcontractors (subcontractor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_contracts_created_at_updated_at" ON gccs.contracts (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_contracts_tenant_id_contract_number" ON gccs.contracts (tenant_id, contract_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_contracts_tenant_id_status" ON gccs.contracts (tenant_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_control_assessments_control_id" ON gccs.control_assessments (control_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_controls_framework_cmmc_level" ON gccs.controls (framework, cmmc_level);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_employees_created_at_updated_at" ON gccs.employees (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_employees_tenant_id_email" ON gccs.employees (tenant_id, email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_employees_tenant_id_employee_number" ON gccs.employees (tenant_id, employee_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_evidence_contracts_contract_id" ON gccs.evidence_contracts (contract_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_evidence_controls_control_id" ON gccs.evidence_controls (control_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_evidence_employees_employee_id" ON gccs.evidence_employees (employee_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_evidence_items_created_at_updated_at" ON gccs.evidence_items (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_evidence_items_tenant_id_expires_at" ON gccs.evidence_items (tenant_id, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_evidence_items_tenant_id_status" ON gccs.evidence_items (tenant_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_evidence_obligations_obligation_id" ON gccs.evidence_obligations (obligation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_evidence_vendors_vendor_id" ON gccs.evidence_vendors (vendor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_flow_down_clauses_subcontractor_id_clause_number" ON gccs.flow_down_clauses (subcontractor_id, clause_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_labor_category_rates_wage_determination_id" ON gccs.labor_category_rates (wage_determination_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_labor_classifications_created_at_updated_at" ON gccs.labor_classifications (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_labor_classifications_tenant_id_employee_id_contract_id" ON gccs.labor_classifications (tenant_id, employee_id, contract_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_obligations_source" ON gccs.obligations (source);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_payroll_records_created_at_updated_at" ON gccs.payroll_records (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_payroll_records_tenant_id_employee_id_period_start_period_e~" ON gccs.payroll_records (tenant_id, employee_id, period_start, period_end);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_poam_evidence_evidence_item_id" ON gccs.poam_evidence (evidence_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_poam_items_created_at_updated_at" ON gccs.poam_items (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_poam_items_tenant_id_status_target_completion_at" ON gccs.poam_items (tenant_id, status, target_completion_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_report_contracts_report_entity_id" ON gccs.report_contracts (report_entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_report_evidence_report_entity_id" ON gccs.report_evidence (report_entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_report_obligations_report_entity_id" ON gccs.report_obligations (report_entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_reports_created_at_updated_at" ON gccs.reports (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_reports_tenant_id_type_status" ON gccs.reports (tenant_id, type, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_roles_created_at_updated_at" ON gccs.roles (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_roles_tenant_id_name" ON gccs.roles (tenant_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_solicitations_created_at_updated_at" ON gccs.solicitations (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_solicitations_tenant_id_solicitation_number" ON gccs.solicitations (tenant_id, solicitation_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_subcontractor_evidence_evidence_item_id" ON gccs.subcontractor_evidence (evidence_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_subcontractors_created_at_updated_at" ON gccs.subcontractors (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_subcontractors_tenant_id_name" ON gccs.subcontractors (tenant_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_subcontractors_tenant_id_uei" ON gccs.subcontractors (tenant_id, uei);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_system_boundaries_created_at_updated_at" ON gccs.system_boundaries (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_system_boundaries_tenant_id_status" ON gccs.system_boundaries (tenant_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_system_boundary_assets_asset_id" ON gccs.system_boundary_assets (asset_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_system_boundary_evidence_evidence_item_id" ON gccs.system_boundary_evidence (evidence_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_system_boundary_external_service_providers_vendor_id" ON gccs.system_boundary_external_service_providers (vendor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_tenants_created_at_updated_at" ON gccs.tenants (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_training_records_created_at_updated_at" ON gccs.training_records (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_training_records_tenant_id_employee_id_status" ON gccs.training_records (tenant_id, employee_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_training_records_tenant_id_expires_at" ON gccs.training_records (tenant_id, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_user_roles_role_id" ON gccs.user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_users_created_at_updated_at" ON gccs.users (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_users_tenant_id_email" ON gccs.users (tenant_id, email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_vendors_created_at_updated_at" ON gccs.vendors (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_vendors_tenant_id_type" ON gccs.vendors (tenant_id, type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE INDEX "IX_wage_determinations_created_at_updated_at" ON gccs.wage_determinations (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    CREATE UNIQUE INDEX "IX_wage_determinations_tenant_id_determination_number_revision" ON gccs.wage_determinations (tenant_id, determination_number, revision);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610031239_InitialDevelopmentSchema') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260610031239_InitialDevelopmentSchema', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    ALTER TABLE gccs.contract_clauses ADD review_state character varying(64) NOT NULL DEFAULT 'Draft';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    ALTER TABLE gccs.contract_clauses ADD source_hash character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    ALTER TABLE gccs.clauses ADD clause_effective_at date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    ALTER TABLE gccs.clauses ADD clause_text_version character varying(120) NOT NULL DEFAULT 'current';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    ALTER TABLE gccs.clauses ADD review_state character varying(64) NOT NULL DEFAULT 'Draft';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    ALTER TABLE gccs.clauses ADD source_hash character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    ALTER TABLE gccs.clauses ADD superseded_at date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    ALTER TABLE gccs.clauses ADD superseded_by_clause_id character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260610051044_AddClauseReviewVersioning') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260610051044_AddClauseReviewVersioning', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613213418_AddTenantMemberships') THEN
    CREATE TABLE gccs.tenant_memberships (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        user_id uuid NOT NULL,
        status character varying(64) NOT NULL,
        role_name character varying(120) NOT NULL,
        last_accessed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_tenant_memberships" PRIMARY KEY (id),
        CONSTRAINT "FK_tenant_memberships_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_tenant_memberships_users_user_id" FOREIGN KEY (user_id) REFERENCES gccs.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613213418_AddTenantMemberships') THEN
    CREATE INDEX "IX_tenant_memberships_created_at_updated_at" ON gccs.tenant_memberships (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613213418_AddTenantMemberships') THEN
    CREATE INDEX "IX_tenant_memberships_tenant_id_status" ON gccs.tenant_memberships (tenant_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613213418_AddTenantMemberships') THEN
    CREATE UNIQUE INDEX "IX_tenant_memberships_tenant_id_user_id" ON gccs.tenant_memberships (tenant_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613213418_AddTenantMemberships') THEN
    CREATE INDEX "IX_tenant_memberships_user_id" ON gccs.tenant_memberships (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613213418_AddTenantMemberships') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260613213418_AddTenantMemberships', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613221118_AddTenantInvitations') THEN
    CREATE TABLE gccs.tenant_invitations (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        email character varying(320) NOT NULL,
        role_name character varying(120) NOT NULL,
        invitation_token character varying(128) NOT NULL,
        status character varying(64) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        accepted_at timestamp with time zone,
        accepted_by_user_id uuid,
        revoked_at timestamp with time zone,
        revoked_by_user_id uuid,
        notification_sent_at timestamp with time zone,
        notification_placeholder character varying(600) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_tenant_invitations" PRIMARY KEY (id),
        CONSTRAINT "FK_tenant_invitations_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613221118_AddTenantInvitations') THEN
    CREATE INDEX "IX_tenant_invitations_created_at_updated_at" ON gccs.tenant_invitations (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613221118_AddTenantInvitations') THEN
    CREATE UNIQUE INDEX "IX_tenant_invitations_invitation_token" ON gccs.tenant_invitations (invitation_token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613221118_AddTenantInvitations') THEN
    CREATE INDEX "IX_tenant_invitations_tenant_id_email_status" ON gccs.tenant_invitations (tenant_id, email, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613221118_AddTenantInvitations') THEN
    CREATE INDEX "IX_tenant_invitations_tenant_id_status_expires_at" ON gccs.tenant_invitations (tenant_id, status, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260613221118_AddTenantInvitations') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260613221118_AddTenantInvitations', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615003848_AddNoCuiAcknowledgements') THEN
    ALTER TABLE gccs.audit_log_entries DROP CONSTRAINT "FK_audit_log_entries_tenants_tenant_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615003848_AddNoCuiAcknowledgements') THEN
    CREATE TABLE gccs.no_cui_acknowledgements (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        user_id uuid NOT NULL,
        notice_version character varying(80) NOT NULL,
        notice_copy character varying(1000) NOT NULL,
        acknowledged_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_no_cui_acknowledgements" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615003848_AddNoCuiAcknowledgements') THEN
    CREATE INDEX "IX_no_cui_acknowledgements_created_at_updated_at" ON gccs.no_cui_acknowledgements (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615003848_AddNoCuiAcknowledgements') THEN
    CREATE UNIQUE INDEX "IX_no_cui_acknowledgements_tenant_id_user_id_notice_version" ON gccs.no_cui_acknowledgements (tenant_id, user_id, notice_version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615003848_AddNoCuiAcknowledgements') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615003848_AddNoCuiAcknowledgements', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615005659_AddEvidenceUploadGuardrails') THEN
    ALTER TABLE gccs.evidence_items ADD content_type character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615005659_AddEvidenceUploadGuardrails') THEN
    ALTER TABLE gccs.evidence_items ADD malware_scan_status character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615005659_AddEvidenceUploadGuardrails') THEN
    ALTER TABLE gccs.evidence_items ADD original_file_name character varying(240);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615005659_AddEvidenceUploadGuardrails') THEN
    ALTER TABLE gccs.evidence_items ADD size_bytes bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615005659_AddEvidenceUploadGuardrails') THEN
    ALTER TABLE gccs.evidence_items ADD upload_validation_status character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615005659_AddEvidenceUploadGuardrails') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615005659_AddEvidenceUploadGuardrails', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615010139_AddAuditRequestMetadata') THEN
    ALTER TABLE gccs.audit_log_entries ADD correlation_id character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615010139_AddAuditRequestMetadata') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615010139_AddAuditRequestMetadata', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615011257_AddObligationPublicationMetadata') THEN
    ALTER TABLE gccs.obligations ADD confidence text NOT NULL DEFAULT 'unknown';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615011257_AddObligationPublicationMetadata') THEN
    ALTER TABLE gccs.obligations ADD last_reviewed_at date NOT NULL DEFAULT DATE '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615011257_AddObligationPublicationMetadata') THEN
    ALTER TABLE gccs.obligations ADD next_review_due_at date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615011257_AddObligationPublicationMetadata') THEN
    ALTER TABLE gccs.obligations ADD requires_expert_review boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615011257_AddObligationPublicationMetadata') THEN
    ALTER TABLE gccs.obligations ADD requires_flow_down boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615011257_AddObligationPublicationMetadata') THEN
    ALTER TABLE gccs.obligations ADD review_state character varying(64) NOT NULL DEFAULT 'Draft';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615011257_AddObligationPublicationMetadata') THEN
    ALTER TABLE gccs.obligations ADD reviewed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615011257_AddObligationPublicationMetadata') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615011257_AddObligationPublicationMetadata', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615032303_AddContractRecordFields') THEN
    ALTER TABLE gccs.contracts ALTER COLUMN place_of_performance TYPE character varying(240);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615032303_AddContractRecordFields') THEN
    ALTER TABLE gccs.contracts ALTER COLUMN agency_or_prime_name TYPE character varying(240);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615032303_AddContractRecordFields') THEN
    ALTER TABLE gccs.contracts ADD data_handling_posture character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615032303_AddContractRecordFields') THEN
    ALTER TABLE gccs.contracts ADD description character varying(1200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615032303_AddContractRecordFields') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615032303_AddContractRecordFields', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615033018_AddContractDocumentUploadMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD content_type character varying(160) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615033018_AddContractDocumentUploadMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD malware_scan_status character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615033018_AddContractDocumentUploadMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD notice_version character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615033018_AddContractDocumentUploadMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD size_bytes bigint NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615033018_AddContractDocumentUploadMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD validation_status character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615033018_AddContractDocumentUploadMetadata') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615033018_AddContractDocumentUploadMetadata', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615040552_AddClauseTenantScope') THEN
    DROP INDEX gccs."IX_clauses_source_number";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615040552_AddClauseTenantScope') THEN
    ALTER TABLE gccs.clauses ADD tenant_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615040552_AddClauseTenantScope') THEN
    CREATE INDEX "IX_clauses_tenant_id_review_state" ON gccs.clauses (tenant_id, review_state);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615040552_AddClauseTenantScope') THEN
    CREATE UNIQUE INDEX "IX_clauses_tenant_id_source_number" ON gccs.clauses (tenant_id, source, number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615040552_AddClauseTenantScope') THEN
    ALTER TABLE gccs.clauses ADD CONSTRAINT "FK_clauses_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615040552_AddClauseTenantScope') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615040552_AddClauseTenantScope', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD attachment_reason character varying(600) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD clause_library_id character varying(160) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD created_at timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD created_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD removal_reason character varying(600);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD removed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD removed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD source_document_reference character varying(300);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD source_url character varying(600) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD updated_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    ALTER TABLE gccs.contract_clauses ADD updated_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    CREATE INDEX "IX_contract_clauses_contract_id_clause_library_id_removed_at" ON gccs.contract_clauses (contract_id, clause_library_id, removed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    CREATE INDEX "IX_contract_clauses_created_at_updated_at" ON gccs.contract_clauses (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615041300_AddContractClauseAttachmentWorkflow') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615041300_AddContractClauseAttachmentWorkflow', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615050151_AddEvidenceMetadataOwner') THEN
    ALTER TABLE gccs.evidence_items ADD owner_function character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615050151_AddEvidenceMetadataOwner') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615050151_AddEvidenceMetadataOwner', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615050609_AddEvidenceFileVersions') THEN
    CREATE TABLE gccs.evidence_file_versions (
        id uuid NOT NULL,
        evidence_item_id uuid NOT NULL,
        version_number integer NOT NULL,
        file_name character varying(240) NOT NULL,
        content_type character varying(160) NOT NULL,
        size_bytes bigint NOT NULL,
        validation_status character varying(80) NOT NULL,
        malware_scan_status character varying(80) NOT NULL,
        storage_uri text,
        file_hash text,
        uploaded_at timestamp with time zone NOT NULL,
        uploaded_by_user_id uuid NOT NULL,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT "PK_evidence_file_versions" PRIMARY KEY (id),
        CONSTRAINT "FK_evidence_file_versions_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615050609_AddEvidenceFileVersions') THEN
    CREATE UNIQUE INDEX "IX_evidence_file_versions_evidence_item_id_version_number" ON gccs.evidence_file_versions (evidence_item_id, version_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615050609_AddEvidenceFileVersions') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615050609_AddEvidenceFileVersions', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615051830_AddCmmcAssessmentMetadata') THEN
    ALTER TABLE gccs.assessments ADD company_profile_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615051830_AddCmmcAssessmentMetadata') THEN
    ALTER TABLE gccs.assessments ADD contract_ids_json jsonb NOT NULL DEFAULT ('[]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615051830_AddCmmcAssessmentMetadata') THEN
    ALTER TABLE gccs.assessments ADD framework character varying(120) NOT NULL DEFAULT 'CMMC';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615051830_AddCmmcAssessmentMetadata') THEN
    ALTER TABLE gccs.assessments ADD name character varying(240) NOT NULL DEFAULT 'CMMC readiness assessment';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615051830_AddCmmcAssessmentMetadata') THEN
    ALTER TABLE gccs.assessments ADD owner_function character varying(120) NOT NULL DEFAULT 'Compliance';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615051830_AddCmmcAssessmentMetadata') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615051830_AddCmmcAssessmentMetadata', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615052651_AddCmmcControlReadinessLinks') THEN
    ALTER TABLE gccs.control_assessments ADD asset_ids_json jsonb NOT NULL DEFAULT ('[]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615052651_AddCmmcControlReadinessLinks') THEN
    ALTER TABLE gccs.control_assessments ADD poam_item_ids_json jsonb NOT NULL DEFAULT ('[]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615052651_AddCmmcControlReadinessLinks') THEN
    ALTER TABLE gccs.control_assessments ADD task_ids_json jsonb NOT NULL DEFAULT ('[]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615052651_AddCmmcControlReadinessLinks') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615052651_AddCmmcControlReadinessLinks', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615053503_AddCmmcPoamRemediationFields') THEN
    ALTER TABLE gccs.poam_items ADD assessment_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615053503_AddCmmcPoamRemediationFields') THEN
    ALTER TABLE gccs.poam_items ADD owner_function character varying(120) NOT NULL DEFAULT 'Security';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615053503_AddCmmcPoamRemediationFields') THEN
    ALTER TABLE gccs.poam_items ADD remediation_task_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615053503_AddCmmcPoamRemediationFields') THEN
    CREATE INDEX "IX_poam_items_assessment_id_control_id" ON gccs.poam_items (assessment_id, control_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615053503_AddCmmcPoamRemediationFields') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615053503_AddCmmcPoamRemediationFields', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054314_AddCmmcAffirmationEvidenceAudit') THEN
    ALTER TABLE gccs.annual_affirmations ADD created_at timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054314_AddCmmcAffirmationEvidenceAudit') THEN
    ALTER TABLE gccs.annual_affirmations ADD created_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054314_AddCmmcAffirmationEvidenceAudit') THEN
    ALTER TABLE gccs.annual_affirmations ADD evidence_item_ids_json jsonb NOT NULL DEFAULT ('[]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054314_AddCmmcAffirmationEvidenceAudit') THEN
    ALTER TABLE gccs.annual_affirmations ADD updated_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054314_AddCmmcAffirmationEvidenceAudit') THEN
    ALTER TABLE gccs.annual_affirmations ADD updated_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054314_AddCmmcAffirmationEvidenceAudit') THEN
    CREATE INDEX "IX_annual_affirmations_created_at_updated_at" ON gccs.annual_affirmations (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054314_AddCmmcAffirmationEvidenceAudit') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615054314_AddCmmcAffirmationEvidenceAudit', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054830_AddSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD cmmc_status character varying(120) NOT NULL DEFAULT 'Unknown';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054830_AddSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD has_export_controlled_access boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054830_AddSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD insurance_expires_at date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054830_AddSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD nda_status character varying(120) NOT NULL DEFAULT 'NotOnFile';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054830_AddSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD role_description character varying(160) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054830_AddSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD small_business_status character varying(120) NOT NULL DEFAULT 'Unknown';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615054830_AddSubcontractorProfileFields') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615054830_AddSubcontractorProfileFields', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD acknowledged_at date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD contract_clause_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD contract_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD created_at timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD created_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD obligation_id character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD updated_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD updated_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD waived_at date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    CREATE INDEX "IX_flow_down_clauses_contract_clause_id" ON gccs.flow_down_clauses (contract_clause_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    CREATE INDEX "IX_flow_down_clauses_contract_id_clause_number" ON gccs.flow_down_clauses (contract_id, clause_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    CREATE INDEX "IX_flow_down_clauses_created_at_updated_at" ON gccs.flow_down_clauses (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    CREATE INDEX "IX_flow_down_clauses_obligation_id" ON gccs.flow_down_clauses (obligation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    CREATE INDEX "IX_flow_down_clauses_signed_evidence_item_id" ON gccs.flow_down_clauses (signed_evidence_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    CREATE INDEX "IX_flow_down_clauses_subcontractor_id_contract_id" ON gccs.flow_down_clauses (subcontractor_id, contract_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD CONSTRAINT "FK_flow_down_clauses_contract_clauses_contract_clause_id" FOREIGN KEY (contract_clause_id) REFERENCES gccs.contract_clauses (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD CONSTRAINT "FK_flow_down_clauses_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD CONSTRAINT "FK_flow_down_clauses_evidence_items_signed_evidence_item_id" FOREIGN KEY (signed_evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    ALTER TABLE gccs.flow_down_clauses ADD CONSTRAINT "FK_flow_down_clauses_obligations_obligation_id" FOREIGN KEY (obligation_id) REFERENCES gccs.obligations (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615181531_AddSubcontractorFlowDownTracking') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615181531_AddSubcontractorFlowDownTracking', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183051_AddSubcontractorEvidenceRequests') THEN
    CREATE TABLE gccs.subcontractor_evidence_requests (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        subcontractor_id uuid NOT NULL,
        requested_item character varying(300) NOT NULL,
        requested_evidence_types_json jsonb NOT NULL,
        due_date date NOT NULL,
        status character varying(64) NOT NULL,
        recipient_name character varying(160),
        recipient_email character varying(320),
        obligation_id character varying(160),
        related_flow_down_clause_id uuid,
        received_evidence_item_id uuid,
        completed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_subcontractor_evidence_requests" PRIMARY KEY (id),
        CONSTRAINT "FK_subcontractor_evidence_requests_evidence_items_received_evi~" FOREIGN KEY (received_evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_subcontractor_evidence_requests_flow_down_clauses_related_f~" FOREIGN KEY (related_flow_down_clause_id) REFERENCES gccs.flow_down_clauses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_subcontractor_evidence_requests_obligations_obligation_id" FOREIGN KEY (obligation_id) REFERENCES gccs.obligations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_subcontractor_evidence_requests_subcontractors_subcontracto~" FOREIGN KEY (subcontractor_id) REFERENCES gccs.subcontractors (id) ON DELETE CASCADE,
        CONSTRAINT "FK_subcontractor_evidence_requests_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183051_AddSubcontractorEvidenceRequests') THEN
    CREATE INDEX "IX_subcontractor_evidence_requests_created_at_updated_at" ON gccs.subcontractor_evidence_requests (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183051_AddSubcontractorEvidenceRequests') THEN
    CREATE INDEX "IX_subcontractor_evidence_requests_obligation_id" ON gccs.subcontractor_evidence_requests (obligation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183051_AddSubcontractorEvidenceRequests') THEN
    CREATE INDEX "IX_subcontractor_evidence_requests_received_evidence_item_id" ON gccs.subcontractor_evidence_requests (received_evidence_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183051_AddSubcontractorEvidenceRequests') THEN
    CREATE INDEX "IX_subcontractor_evidence_requests_related_flow_down_clause_id" ON gccs.subcontractor_evidence_requests (related_flow_down_clause_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183051_AddSubcontractorEvidenceRequests') THEN
    CREATE INDEX "IX_subcontractor_evidence_requests_subcontractor_id_due_date" ON gccs.subcontractor_evidence_requests (subcontractor_id, due_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183051_AddSubcontractorEvidenceRequests') THEN
    CREATE INDEX "IX_subcontractor_evidence_requests_tenant_id_status_due_date" ON gccs.subcontractor_evidence_requests (tenant_id, status, due_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183051_AddSubcontractorEvidenceRequests') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615183051_AddSubcontractorEvidenceRequests', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183901_AddComplianceStatusReportSnapshots') THEN
    ALTER TABLE gccs.reports ADD export_html text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183901_AddComplianceStatusReportSnapshots') THEN
    ALTER TABLE gccs.reports ADD snapshot_json jsonb NOT NULL DEFAULT '{}';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615183901_AddComplianceStatusReportSnapshots') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615183901_AddComplianceStatusReportSnapshots', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615191714_AddNotificationPreferences') THEN
    CREATE TABLE gccs.notification_preferences (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        user_id uuid NOT NULL,
        role_name character varying(120) NOT NULL,
        assignment_notifications_enabled boolean NOT NULL,
        due_soon_notifications_enabled boolean NOT NULL,
        overdue_notifications_enabled boolean NOT NULL,
        evidence_request_notifications_enabled boolean NOT NULL,
        certification_renewal_notifications_enabled boolean NOT NULL,
        cmmc_affirmation_notifications_enabled boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_notification_preferences" PRIMARY KEY (id),
        CONSTRAINT "FK_notification_preferences_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615191714_AddNotificationPreferences') THEN
    CREATE INDEX "IX_notification_preferences_created_at_updated_at" ON gccs.notification_preferences (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615191714_AddNotificationPreferences') THEN
    CREATE UNIQUE INDEX "IX_notification_preferences_tenant_id_user_id" ON gccs.notification_preferences (tenant_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615191714_AddNotificationPreferences') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615191714_AddNotificationPreferences', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192143_AddNotificationDeliveries') THEN
    CREATE TABLE gccs.notification_deliveries (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        user_id uuid NOT NULL,
        source_task_id uuid NOT NULL,
        category character varying(80) NOT NULL,
        status character varying(80) NOT NULL,
        placeholder character varying(800) NOT NULL,
        failure_message character varying(800),
        attempted_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_notification_deliveries" PRIMARY KEY (id),
        CONSTRAINT "FK_notification_deliveries_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192143_AddNotificationDeliveries') THEN
    CREATE INDEX "IX_notification_deliveries_created_at_updated_at" ON gccs.notification_deliveries (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192143_AddNotificationDeliveries') THEN
    CREATE INDEX "IX_notification_deliveries_tenant_id_attempted_at" ON gccs.notification_deliveries (tenant_id, attempted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192143_AddNotificationDeliveries') THEN
    CREATE UNIQUE INDEX "IX_notification_deliveries_tenant_id_source_task_id_category" ON gccs.notification_deliveries (tenant_id, source_task_id, category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192143_AddNotificationDeliveries') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615192143_AddNotificationDeliveries', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192707_AddAssignmentNotificationMetadata') THEN
    DROP INDEX gccs."IX_notification_deliveries_tenant_id_source_task_id_category";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192707_AddAssignmentNotificationMetadata') THEN
    ALTER TABLE gccs.notification_deliveries ADD link_url character varying(400) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192707_AddAssignmentNotificationMetadata') THEN
    ALTER TABLE gccs.notification_deliveries ADD read_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192707_AddAssignmentNotificationMetadata') THEN
    ALTER TABLE gccs.notification_deliveries ADD source_type character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192707_AddAssignmentNotificationMetadata') THEN
    CREATE UNIQUE INDEX "IX_notification_deliveries_tenant_id_source_task_id_category_user_id" ON gccs.notification_deliveries (tenant_id, source_task_id, category, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260615192707_AddAssignmentNotificationMetadata') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260615192707_AddAssignmentNotificationMetadata', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617211500_AddExtractionJobs') THEN
    CREATE TABLE gccs.extraction_jobs (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        source_document_id uuid NOT NULL,
        requested_by_user_id uuid NOT NULL,
        status character varying(64) NOT NULL,
        requested_at timestamp with time zone NOT NULL,
        started_at timestamp with time zone,
        completed_at timestamp with time zone,
        failure_reason character varying(1000),
        CONSTRAINT "PK_extraction_jobs" PRIMARY KEY (id),
        CONSTRAINT "FK_extraction_jobs_contract_documents_source_document_id" FOREIGN KEY (source_document_id) REFERENCES gccs.contract_documents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_extraction_jobs_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617211500_AddExtractionJobs') THEN
    CREATE INDEX "IX_extraction_jobs_source_document_id" ON gccs.extraction_jobs (source_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617211500_AddExtractionJobs') THEN
    CREATE INDEX "IX_extraction_jobs_tenant_id_status_requested_at" ON gccs.extraction_jobs (tenant_id, status, requested_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617211500_AddExtractionJobs') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617211500_AddExtractionJobs', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617212012_AddClauseExtractionCandidates') THEN
    CREATE TABLE gccs.clause_candidates (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        extraction_job_id uuid NOT NULL,
        source_document_id uuid NOT NULL,
        normalized_citation character varying(120) NOT NULL,
        raw_extracted_text character varying(2000) NOT NULL,
        detected_title character varying(300),
        confidence numeric(5,4) NOT NULL,
        location_metadata character varying(300) NOT NULL,
        match_method character varying(80) NOT NULL,
        clause_library_id character varying(160),
        review_status character varying(80) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_clause_candidates" PRIMARY KEY (id),
        CONSTRAINT "FK_clause_candidates_contract_documents_source_document_id" FOREIGN KEY (source_document_id) REFERENCES gccs.contract_documents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_clause_candidates_extraction_jobs_extraction_job_id" FOREIGN KEY (extraction_job_id) REFERENCES gccs.extraction_jobs (id) ON DELETE CASCADE,
        CONSTRAINT "FK_clause_candidates_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617212012_AddClauseExtractionCandidates') THEN
    CREATE INDEX "IX_clause_candidates_extraction_job_id_normalized_citation" ON gccs.clause_candidates (extraction_job_id, normalized_citation);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617212012_AddClauseExtractionCandidates') THEN
    CREATE INDEX "IX_clause_candidates_source_document_id" ON gccs.clause_candidates (source_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617212012_AddClauseExtractionCandidates') THEN
    CREATE INDEX "IX_clause_candidates_tenant_id_source_document_id" ON gccs.clause_candidates (tenant_id, source_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617212012_AddClauseExtractionCandidates') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617212012_AddClauseExtractionCandidates', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617213149_AddClauseCandidateReviewMetadata') THEN
    ALTER TABLE gccs.clause_candidates ADD decision_note character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617213149_AddClauseCandidateReviewMetadata') THEN
    ALTER TABLE gccs.clause_candidates ADD decision_reason character varying(600);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617213149_AddClauseCandidateReviewMetadata') THEN
    ALTER TABLE gccs.clause_candidates ADD reviewed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617213149_AddClauseCandidateReviewMetadata') THEN
    ALTER TABLE gccs.clause_candidates ADD reviewed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617213149_AddClauseCandidateReviewMetadata') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617213149_AddClauseCandidateReviewMetadata', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617223849_AddSuggestedObligations') THEN
    CREATE TABLE gccs.suggested_obligations (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        source character varying(160) NOT NULL,
        source_url character varying(600) NOT NULL,
        generated_summary character varying(2000) NOT NULL,
        proposed_title character varying(300) NOT NULL,
        proposed_owner_function character varying(120) NOT NULL,
        required_action character varying(1200) NOT NULL,
        risk_level character varying(64) NOT NULL,
        evidence_suggestions_json jsonb NOT NULL,
        source_citations_json jsonb NOT NULL,
        confidence character varying(80) NOT NULL,
        prompt_version character varying(120) NOT NULL,
        model_identifier character varying(160) NOT NULL,
        retrieved_source_references_json jsonb NOT NULL,
        review_status character varying(80) NOT NULL,
        created_by_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        reviewed_by_user_id uuid,
        reviewed_at timestamp with time zone,
        review_reason character varying(1000),
        CONSTRAINT "PK_suggested_obligations" PRIMARY KEY (id),
        CONSTRAINT "FK_suggested_obligations_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617223849_AddSuggestedObligations') THEN
    CREATE INDEX "IX_suggested_obligations_tenant_id_review_status_created_at" ON gccs.suggested_obligations (tenant_id, review_status, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617223849_AddSuggestedObligations') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617223849_AddSuggestedObligations', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617224252_AddExpertReviewQueue') THEN
    CREATE TABLE gccs.expert_review_items (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        source_type character varying(80) NOT NULL,
        source_id uuid NOT NULL,
        reason character varying(1000) NOT NULL,
        priority character varying(40) NOT NULL,
        topic character varying(240) NOT NULL,
        assigned_expert_user_id uuid,
        due_at date,
        status character varying(80) NOT NULL,
        created_by_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        resolved_by_user_id uuid,
        resolved_at timestamp with time zone,
        resolution_decision character varying(120),
        resolution_notes character varying(1000),
        CONSTRAINT "PK_expert_review_items" PRIMARY KEY (id),
        CONSTRAINT "FK_expert_review_items_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617224252_AddExpertReviewQueue') THEN
    CREATE INDEX "IX_expert_review_items_tenant_id_assigned_expert_user_id_due_at" ON gccs.expert_review_items (tenant_id, assigned_expert_user_id, due_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617224252_AddExpertReviewQueue') THEN
    CREATE INDEX "IX_expert_review_items_tenant_id_status_source_type" ON gccs.expert_review_items (tenant_id, status, source_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617224252_AddExpertReviewQueue') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617224252_AddExpertReviewQueue', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617225316_AddClauseObligationMappings') THEN
    CREATE TABLE gccs.clause_obligation_mappings (
        id uuid NOT NULL,
        tenant_id uuid,
        clause_id character varying(160) NOT NULL,
        obligation_id character varying(160) NOT NULL,
        trigger_condition character varying(1200) NOT NULL,
        required_action character varying(1200) NOT NULL,
        source_url character varying(600) NOT NULL,
        confidence character varying(80) NOT NULL,
        requires_expert_review boolean NOT NULL,
        review_state character varying(64) NOT NULL DEFAULT 'Draft',
        last_reviewed_at date NOT NULL,
        reviewed_by_user_id uuid,
        previous_mapping_id uuid,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_clause_obligation_mappings" PRIMARY KEY (id),
        CONSTRAINT "FK_clause_obligation_mappings_clauses_clause_id" FOREIGN KEY (clause_id) REFERENCES gccs.clauses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_clause_obligation_mappings_obligations_obligation_id" FOREIGN KEY (obligation_id) REFERENCES gccs.obligations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_clause_obligation_mappings_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617225316_AddClauseObligationMappings') THEN
    CREATE INDEX "IX_clause_obligation_mappings_clause_id_review_state" ON gccs.clause_obligation_mappings (clause_id, review_state);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617225316_AddClauseObligationMappings') THEN
    CREATE INDEX "IX_clause_obligation_mappings_obligation_id" ON gccs.clause_obligation_mappings (obligation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617225316_AddClauseObligationMappings') THEN
    CREATE INDEX "IX_clause_obligation_mappings_tenant_id_clause_id_obligation_id" ON gccs.clause_obligation_mappings (tenant_id, clause_id, obligation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617225316_AddClauseObligationMappings') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617225316_AddClauseObligationMappings', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617230321_AddObligationApplicabilityEvaluations') THEN
    CREATE TABLE gccs.obligation_applicability_evaluations (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        contract_clause_id uuid NOT NULL,
        obligation_id character varying(160) NOT NULL,
        previous_evaluation_id uuid,
        source_rule_id character varying(160) NOT NULL,
        state character varying(80) NOT NULL,
        explanation character varying(2000) NOT NULL,
        facts_used_json jsonb NOT NULL,
        missing_facts_json jsonb NOT NULL,
        metadata_json jsonb NOT NULL,
        evaluated_at timestamp with time zone NOT NULL,
        evaluated_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_obligation_applicability_evaluations" PRIMARY KEY (id),
        CONSTRAINT "FK_obligation_applicability_evaluations_contract_clause_obliga~" FOREIGN KEY (contract_clause_id, obligation_id) REFERENCES gccs.contract_clause_obligations (contract_clause_id, obligation_id) ON DELETE CASCADE,
        CONSTRAINT "FK_obligation_applicability_evaluations_obligation_applicabili~" FOREIGN KEY (previous_evaluation_id) REFERENCES gccs.obligation_applicability_evaluations (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_obligation_applicability_evaluations_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617230321_AddObligationApplicabilityEvaluations') THEN
    CREATE INDEX "IX_obligation_applicability_evaluations_contract_clause_id_obl~" ON gccs.obligation_applicability_evaluations (contract_clause_id, obligation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617230321_AddObligationApplicabilityEvaluations') THEN
    CREATE INDEX "IX_obligation_applicability_evaluations_previous_evaluation_id" ON gccs.obligation_applicability_evaluations (previous_evaluation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617230321_AddObligationApplicabilityEvaluations') THEN
    CREATE INDEX "IX_obligation_applicability_evaluations_tenant_id_contract_cla~" ON gccs.obligation_applicability_evaluations (tenant_id, contract_clause_id, obligation_id, evaluated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617230321_AddObligationApplicabilityEvaluations') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617230321_AddObligationApplicabilityEvaluations', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617231719_AddSubcontractorSamMetadata') THEN
    ALTER TABLE gccs.subcontractors ADD sam_exclusion_status character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617231719_AddSubcontractorSamMetadata') THEN
    ALTER TABLE gccs.subcontractors ADD sam_naics_json jsonb NOT NULL DEFAULT '[]';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617231719_AddSubcontractorSamMetadata') THEN
    ALTER TABLE gccs.subcontractors ADD sam_registration_expires_at date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617231719_AddSubcontractorSamMetadata') THEN
    ALTER TABLE gccs.subcontractors ADD sam_registration_status character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617231719_AddSubcontractorSamMetadata') THEN
    ALTER TABLE gccs.subcontractors ADD sam_retrieved_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617231719_AddSubcontractorSamMetadata') THEN
    ALTER TABLE gccs.subcontractors ADD sam_source character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617231719_AddSubcontractorSamMetadata') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617231719_AddSubcontractorSamMetadata', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617232240_AddSbaSizeStandards') THEN
    CREATE TABLE gccs.sba_size_standards (
        id uuid NOT NULL,
        naics_code character varying(16) NOT NULL,
        metric character varying(80) NOT NULL,
        threshold numeric(18,2) NOT NULL,
        unit character varying(80) NOT NULL,
        source_url character varying(600) NOT NULL,
        effective_at date NOT NULL,
        last_reviewed_at date NOT NULL,
        status character varying(64) NOT NULL DEFAULT 'Draft',
        reviewed_by_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_sba_size_standards" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617232240_AddSbaSizeStandards') THEN
    CREATE INDEX "IX_sba_size_standards_naics_code_status" ON gccs.sba_size_standards (naics_code, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617232240_AddSbaSizeStandards') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617232240_AddSbaSizeStandards', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617232821_AddContractSizeChecks') THEN
    CREATE TABLE gccs.contract_size_checks (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        contract_id uuid NOT NULL,
        naics_code character varying(16) NOT NULL,
        result character varying(80) NOT NULL,
        metric character varying(80) NOT NULL,
        threshold numeric(18,2),
        unit character varying(80),
        entered_value numeric,
        missing_information_json jsonb NOT NULL,
        source_url character varying(600),
        source_effective_at date,
        source_last_reviewed_at date,
        expert_review_task_id uuid,
        run_at timestamp with time zone NOT NULL,
        run_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_contract_size_checks" PRIMARY KEY (id),
        CONSTRAINT "FK_contract_size_checks_compliance_tasks_expert_review_task_id" FOREIGN KEY (expert_review_task_id) REFERENCES gccs.compliance_tasks (id) ON DELETE SET NULL,
        CONSTRAINT "FK_contract_size_checks_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE,
        CONSTRAINT "FK_contract_size_checks_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617232821_AddContractSizeChecks') THEN
    CREATE INDEX "IX_contract_size_checks_contract_id" ON gccs.contract_size_checks (contract_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617232821_AddContractSizeChecks') THEN
    CREATE INDEX "IX_contract_size_checks_expert_review_task_id" ON gccs.contract_size_checks (expert_review_task_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617232821_AddContractSizeChecks') THEN
    CREATE INDEX "IX_contract_size_checks_tenant_id_contract_id_run_at" ON gccs.contract_size_checks (tenant_id, contract_id, run_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617232821_AddContractSizeChecks') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617232821_AddContractSizeChecks', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617233207_AddExpandedSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD certifications_json jsonb NOT NULL DEFAULT '[]';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617233207_AddExpandedSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD naics_codes_json jsonb NOT NULL DEFAULT '[]';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617233207_AddExpandedSubcontractorProfileFields') THEN
    ALTER TABLE gccs.subcontractors ADD owner_function character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617233207_AddExpandedSubcontractorProfileFields') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617233207_AddExpandedSubcontractorProfileFields', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617234344_AddSupplierObligationOwnerFunction') THEN
    ALTER TABLE gccs.subcontractor_evidence_requests ADD owner_function character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617234344_AddSupplierObligationOwnerFunction') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617234344_AddSupplierObligationOwnerFunction', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617234942_AddPolicyTemplates') THEN
    CREATE TABLE gccs.policy_templates (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        title character varying(240) NOT NULL,
        category character varying(120) NOT NULL,
        body text NOT NULL,
        placeholders_json jsonb NOT NULL,
        source_references_json jsonb NOT NULL,
        version character varying(80) NOT NULL,
        status character varying(40) NOT NULL,
        owner_function character varying(160) NOT NULL,
        last_reviewed_at date,
        reviewer_user_id uuid,
        requires_expert_review boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_policy_templates" PRIMARY KEY (id),
        CONSTRAINT "FK_policy_templates_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617234942_AddPolicyTemplates') THEN
    CREATE TABLE gccs.policy_template_versions (
        id uuid NOT NULL,
        template_id uuid NOT NULL,
        version character varying(80) NOT NULL,
        body_preview character varying(500) NOT NULL,
        status character varying(40) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_policy_template_versions" PRIMARY KEY (id),
        CONSTRAINT "FK_policy_template_versions_policy_templates_template_id" FOREIGN KEY (template_id) REFERENCES gccs.policy_templates (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617234942_AddPolicyTemplates') THEN
    CREATE INDEX "IX_policy_template_versions_template_id_version" ON gccs.policy_template_versions (template_id, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617234942_AddPolicyTemplates') THEN
    CREATE INDEX "IX_policy_templates_created_at_updated_at" ON gccs.policy_templates (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617234942_AddPolicyTemplates') THEN
    CREATE INDEX "IX_policy_templates_tenant_id_status_category" ON gccs.policy_templates (tenant_id, status, category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617234942_AddPolicyTemplates') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617234942_AddPolicyTemplates', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235335_AddGeneratedPolicies') THEN
    CREATE TABLE gccs.generated_policies (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        source_template_id uuid NOT NULL,
        source_template_version character varying(80) NOT NULL,
        generated_at timestamp with time zone NOT NULL,
        title character varying(240) NOT NULL,
        body text NOT NULL,
        status character varying(40) NOT NULL,
        placeholder_values_json jsonb NOT NULL,
        missing_placeholders_json jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_generated_policies" PRIMARY KEY (id),
        CONSTRAINT "FK_generated_policies_policy_templates_source_template_id" FOREIGN KEY (source_template_id) REFERENCES gccs.policy_templates (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_generated_policies_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235335_AddGeneratedPolicies') THEN
    CREATE INDEX "IX_generated_policies_created_at_updated_at" ON gccs.generated_policies (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235335_AddGeneratedPolicies') THEN
    CREATE INDEX "IX_generated_policies_source_template_id" ON gccs.generated_policies (source_template_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235335_AddGeneratedPolicies') THEN
    CREATE INDEX "IX_generated_policies_tenant_id_status_generated_at" ON gccs.generated_policies (tenant_id, status, generated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235335_AddGeneratedPolicies') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617235335_AddGeneratedPolicies', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    ALTER TABLE gccs.generated_policies ADD approved_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    ALTER TABLE gccs.generated_policies ADD approved_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    ALTER TABLE gccs.generated_policies ADD evidence_item_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    ALTER TABLE gccs.generated_policies ADD review_due_at date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    CREATE TABLE gccs.policy_revisions (
        id uuid NOT NULL,
        generated_policy_id uuid NOT NULL,
        title character varying(240) NOT NULL,
        body text NOT NULL,
        status character varying(40) NOT NULL,
        preserved_at timestamp with time zone NOT NULL,
        preserved_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_policy_revisions" PRIMARY KEY (id),
        CONSTRAINT "FK_policy_revisions_generated_policies_generated_policy_id" FOREIGN KEY (generated_policy_id) REFERENCES gccs.generated_policies (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    CREATE INDEX "IX_generated_policies_evidence_item_id" ON gccs.generated_policies (evidence_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    CREATE INDEX "IX_policy_revisions_generated_policy_id_preserved_at" ON gccs.policy_revisions (generated_policy_id, preserved_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    ALTER TABLE gccs.generated_policies ADD CONSTRAINT "FK_generated_policies_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260617235748_AddGeneratedPolicyApproval') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260617235748_AddGeneratedPolicyApproval', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000248_AddEvidenceRequests') THEN
    CREATE TABLE gccs.evidence_requests (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        requester_user_id uuid NOT NULL,
        assignee_user_id uuid,
        assignee_subcontractor_id uuid,
        due_date date NOT NULL,
        status character varying(40) NOT NULL,
        instructions character varying(2000) NOT NULL,
        related_record_type character varying(80) NOT NULL,
        related_record_id character varying(160) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        CONSTRAINT "PK_evidence_requests" PRIMARY KEY (id),
        CONSTRAINT "FK_evidence_requests_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000248_AddEvidenceRequests') THEN
    CREATE INDEX "IX_evidence_requests_created_at_updated_at" ON gccs.evidence_requests (created_at, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000248_AddEvidenceRequests') THEN
    CREATE INDEX "IX_evidence_requests_tenant_id_status_due_date" ON gccs.evidence_requests (tenant_id, status, due_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000248_AddEvidenceRequests') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618000248_AddEvidenceRequests', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000632_AddEvidenceRequestSubmissionReview') THEN
    ALTER TABLE gccs.evidence_requests ADD review_comment character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000632_AddEvidenceRequestSubmissionReview') THEN
    ALTER TABLE gccs.evidence_requests ADD reviewed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000632_AddEvidenceRequestSubmissionReview') THEN
    ALTER TABLE gccs.evidence_requests ADD submission_comment character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000632_AddEvidenceRequestSubmissionReview') THEN
    ALTER TABLE gccs.evidence_requests ADD submitted_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000632_AddEvidenceRequestSubmissionReview') THEN
    ALTER TABLE gccs.evidence_requests ADD submitted_evidence_item_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618000632_AddEvidenceRequestSubmissionReview') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618000632_AddEvidenceRequestSubmissionReview', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001008_AddEvidenceRequestDashboardPriority') THEN
    ALTER TABLE gccs.evidence_requests ADD priority character varying(40) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001008_AddEvidenceRequestDashboardPriority') THEN
    CREATE INDEX "IX_evidence_requests_tenant_id_priority" ON gccs.evidence_requests (tenant_id, priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001008_AddEvidenceRequestDashboardPriority') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618001008_AddEvidenceRequestDashboardPriority', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001357_AddCmmcControlAssessmentDetail') THEN
    ALTER TABLE gccs.control_assessments ADD esp_name character varying(240);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001357_AddCmmcControlAssessmentDetail') THEN
    ALTER TABLE gccs.control_assessments ADD esp_responsible boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001357_AddCmmcControlAssessmentDetail') THEN
    ALTER TABLE gccs.control_assessments ADD implementation_details character varying(2000) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001357_AddCmmcControlAssessmentDetail') THEN
    ALTER TABLE gccs.control_assessments ADD inherited_from character varying(240);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001357_AddCmmcControlAssessmentDetail') THEN
    ALTER TABLE gccs.control_assessments ADD is_inherited boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001357_AddCmmcControlAssessmentDetail') THEN
    CREATE TABLE gccs.control_assessment_history (
        id uuid NOT NULL,
        assessment_id uuid NOT NULL,
        control_id text NOT NULL,
        status character varying(64) NOT NULL,
        result character varying(64) NOT NULL,
        notes character varying(1000),
        changed_by_user_id uuid NOT NULL,
        changed_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_control_assessment_history" PRIMARY KEY (id),
        CONSTRAINT "FK_control_assessment_history_control_assessments_assessment_i~" FOREIGN KEY (assessment_id, control_id) REFERENCES gccs.control_assessments (assessment_id, control_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001357_AddCmmcControlAssessmentDetail') THEN
    CREATE INDEX "IX_control_assessment_history_assessment_id_control_id_changed~" ON gccs.control_assessment_history (assessment_id, control_id, changed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618001357_AddCmmcControlAssessmentDetail') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618001357_AddCmmcControlAssessmentDetail', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618002257_AddCmmcResponsibilityMatrix') THEN
    ALTER TABLE gccs.control_assessments ADD owner_function character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618002257_AddCmmcResponsibilityMatrix') THEN
    ALTER TABLE gccs.control_assessments ADD responsibility_notes character varying(1000) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618002257_AddCmmcResponsibilityMatrix') THEN
    ALTER TABLE gccs.control_assessments ADD responsibility_provider character varying(240);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618002257_AddCmmcResponsibilityMatrix') THEN
    ALTER TABLE gccs.control_assessments ADD responsibility_type character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618002257_AddCmmcResponsibilityMatrix') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618002257_AddCmmcResponsibilityMatrix', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618205911_AddTenantDataHandlingModeHistory') THEN
    CREATE TABLE gccs.tenant_data_handling_mode_history (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        previous_mode character varying(64),
        new_mode character varying(64) NOT NULL,
        actor_user_id uuid NOT NULL,
        changed_at timestamp with time zone NOT NULL,
        reason character varying(600) NOT NULL,
        approval_record_reference character varying(160),
        CONSTRAINT "PK_tenant_data_handling_mode_history" PRIMARY KEY (id),
        CONSTRAINT "FK_tenant_data_handling_mode_history_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618205911_AddTenantDataHandlingModeHistory') THEN
    CREATE INDEX "IX_tenant_data_handling_mode_history_tenant_id_changed_at" ON gccs.tenant_data_handling_mode_history (tenant_id, changed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618205911_AddTenantDataHandlingModeHistory') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618205911_AddTenantDataHandlingModeHistory', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.reports ADD classification character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.reports ADD classification_confidence numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.reports ADD classification_is_approved_demo_content boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.reports ADD classification_reason character varying(600);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.reports ADD classification_reviewed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.reports ADD classification_reviewed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.reports ADD classification_source character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.extraction_jobs ADD classification character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.extraction_jobs ADD classification_confidence numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.extraction_jobs ADD classification_is_approved_demo_content boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.extraction_jobs ADD classification_reason character varying(600);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.extraction_jobs ADD classification_reviewed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.extraction_jobs ADD classification_reviewed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.extraction_jobs ADD classification_source character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_items ADD classification character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_items ADD classification_confidence numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_items ADD classification_is_approved_demo_content boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_items ADD classification_reason character varying(600);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_items ADD classification_reviewed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_items ADD classification_reviewed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_items ADD classification_source character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_file_versions ADD classification character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_file_versions ADD classification_confidence numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_file_versions ADD classification_is_approved_demo_content boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_file_versions ADD classification_reason character varying(600);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_file_versions ADD classification_reviewed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_file_versions ADD classification_reviewed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.evidence_file_versions ADD classification_source character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD classification character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD classification_confidence numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD classification_is_approved_demo_content boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD classification_reason character varying(600);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD classification_reviewed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD classification_reviewed_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    ALTER TABLE gccs.contract_documents ADD classification_source character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    CREATE TABLE gccs.content_classification_history (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        entity_type character varying(120) NOT NULL,
        entity_id character varying(120) NOT NULL,
        previous_classification character varying(64),
        new_classification character varying(64) NOT NULL,
        source character varying(64) NOT NULL,
        confidence numeric,
        reviewed_by_user_id uuid,
        reviewed_at timestamp with time zone,
        reason character varying(600),
        changed_by_user_id uuid NOT NULL,
        changed_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_content_classification_history" PRIMARY KEY (id),
        CONSTRAINT "FK_content_classification_history_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES gccs.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    CREATE INDEX "IX_content_classification_history_tenant_id_entity_type_entity~" ON gccs.content_classification_history (tenant_id, entity_type, entity_id, changed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM gccs."__EFMigrationsHistory" WHERE "MigrationId" = '20260618212037_AddContentClassificationMetadata') THEN
    INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618212037_AddContentClassificationMetadata', '10.0.4');
    END IF;
END $EF$;
COMMIT;
