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
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'gccs') THEN
        CREATE SCHEMA gccs;
    END IF;
END $EF$;

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

CREATE TABLE gccs.mvp_modules (
    key text NOT NULL,
    name text NOT NULL,
    purpose text NOT NULL,
    status text NOT NULL,
    CONSTRAINT "PK_mvp_modules" PRIMARY KEY (key)
);

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

CREATE TABLE gccs.evidence_contracts (
    evidence_item_id uuid NOT NULL,
    contract_id uuid NOT NULL,
    CONSTRAINT "PK_evidence_contracts" PRIMARY KEY (evidence_item_id, contract_id),
    CONSTRAINT "FK_evidence_contracts_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE,
    CONSTRAINT "FK_evidence_contracts_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE
);

CREATE TABLE gccs.evidence_controls (
    evidence_item_id uuid NOT NULL,
    control_id text NOT NULL,
    CONSTRAINT "PK_evidence_controls" PRIMARY KEY (evidence_item_id, control_id),
    CONSTRAINT "FK_evidence_controls_controls_control_id" FOREIGN KEY (control_id) REFERENCES gccs.controls (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_evidence_controls_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE
);

CREATE TABLE gccs.evidence_employees (
    evidence_item_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    CONSTRAINT "PK_evidence_employees" PRIMARY KEY (evidence_item_id, employee_id),
    CONSTRAINT "FK_evidence_employees_employees_employee_id" FOREIGN KEY (employee_id) REFERENCES gccs.employees (id) ON DELETE CASCADE,
    CONSTRAINT "FK_evidence_employees_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE
);

CREATE TABLE gccs.evidence_obligations (
    evidence_item_id uuid NOT NULL,
    obligation_id text NOT NULL,
    CONSTRAINT "PK_evidence_obligations" PRIMARY KEY (evidence_item_id, obligation_id),
    CONSTRAINT "FK_evidence_obligations_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
    CONSTRAINT "FK_evidence_obligations_obligations_obligation_id" FOREIGN KEY (obligation_id) REFERENCES gccs.obligations (id) ON DELETE RESTRICT
);

CREATE TABLE gccs.poam_evidence (
    poam_item_id uuid NOT NULL,
    evidence_item_id uuid NOT NULL,
    CONSTRAINT "PK_poam_evidence" PRIMARY KEY (poam_item_id, evidence_item_id),
    CONSTRAINT "FK_poam_evidence_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
    CONSTRAINT "FK_poam_evidence_poam_items_poam_item_id" FOREIGN KEY (poam_item_id) REFERENCES gccs.poam_items (id) ON DELETE CASCADE
);

CREATE TABLE gccs.report_contracts (
    report_id uuid NOT NULL,
    contract_id uuid NOT NULL,
    report_entity_id uuid,
    CONSTRAINT "PK_report_contracts" PRIMARY KEY (report_id, contract_id),
    CONSTRAINT "FK_report_contracts_reports_report_entity_id" FOREIGN KEY (report_entity_id) REFERENCES gccs.reports (id)
);

CREATE TABLE gccs.report_evidence (
    report_id uuid NOT NULL,
    evidence_item_id uuid NOT NULL,
    report_entity_id uuid,
    CONSTRAINT "PK_report_evidence" PRIMARY KEY (report_id, evidence_item_id),
    CONSTRAINT "FK_report_evidence_reports_report_entity_id" FOREIGN KEY (report_entity_id) REFERENCES gccs.reports (id)
);

CREATE TABLE gccs.report_obligations (
    report_id uuid NOT NULL,
    obligation_id text NOT NULL,
    report_entity_id uuid,
    CONSTRAINT "PK_report_obligations" PRIMARY KEY (report_id, obligation_id),
    CONSTRAINT "FK_report_obligations_reports_report_entity_id" FOREIGN KEY (report_entity_id) REFERENCES gccs.reports (id)
);

CREATE TABLE gccs.role_permissions (
    role_id uuid NOT NULL,
    permission character varying(64) NOT NULL,
    CONSTRAINT "PK_role_permissions" PRIMARY KEY (role_id, permission),
    CONSTRAINT "FK_role_permissions_roles_role_id" FOREIGN KEY (role_id) REFERENCES gccs.roles (id) ON DELETE CASCADE
);

CREATE TABLE gccs.contract_subcontractors (
    contract_id uuid NOT NULL,
    subcontractor_id uuid NOT NULL,
    CONSTRAINT "PK_contract_subcontractors" PRIMARY KEY (contract_id, subcontractor_id),
    CONSTRAINT "FK_contract_subcontractors_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE CASCADE,
    CONSTRAINT "FK_contract_subcontractors_subcontractors_subcontractor_id" FOREIGN KEY (subcontractor_id) REFERENCES gccs.subcontractors (id) ON DELETE CASCADE
);

CREATE TABLE gccs.flow_down_clauses (
    id uuid NOT NULL,
    subcontractor_id uuid NOT NULL,
    contract_id uuid,
    contract_clause_id uuid,
    obligation_id character varying(160),
    clause_number text NOT NULL,
    title text NOT NULL,
    status character varying(64) NOT NULL,
    sent_at date,
    acknowledged_at date,
    signed_at date,
    waived_at date,
    signed_evidence_item_id uuid,
    created_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_at timestamp with time zone,
    updated_by_user_id uuid,
    CONSTRAINT "PK_flow_down_clauses" PRIMARY KEY (id),
    CONSTRAINT "FK_flow_down_clauses_contract_clauses_contract_clause_id" FOREIGN KEY (contract_clause_id) REFERENCES gccs.contract_clauses (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_flow_down_clauses_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES gccs.contracts (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_flow_down_clauses_evidence_items_signed_evidence_item_id" FOREIGN KEY (signed_evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_flow_down_clauses_obligations_obligation_id" FOREIGN KEY (obligation_id) REFERENCES gccs.obligations (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_flow_down_clauses_subcontractors_subcontractor_id" FOREIGN KEY (subcontractor_id) REFERENCES gccs.subcontractors (id) ON DELETE CASCADE
);

CREATE TABLE gccs.subcontractor_evidence (
    subcontractor_id uuid NOT NULL,
    evidence_item_id uuid NOT NULL,
    CONSTRAINT "PK_subcontractor_evidence" PRIMARY KEY (subcontractor_id, evidence_item_id),
    CONSTRAINT "FK_subcontractor_evidence_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
    CONSTRAINT "FK_subcontractor_evidence_subcontractors_subcontractor_id" FOREIGN KEY (subcontractor_id) REFERENCES gccs.subcontractors (id) ON DELETE CASCADE
);

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

CREATE TABLE gccs.system_boundary_assets (
    system_boundary_id uuid NOT NULL,
    asset_id uuid NOT NULL,
    CONSTRAINT "PK_system_boundary_assets" PRIMARY KEY (system_boundary_id, asset_id),
    CONSTRAINT "FK_system_boundary_assets_assets_asset_id" FOREIGN KEY (asset_id) REFERENCES gccs.assets (id) ON DELETE CASCADE,
    CONSTRAINT "FK_system_boundary_assets_system_boundaries_system_boundary_id" FOREIGN KEY (system_boundary_id) REFERENCES gccs.system_boundaries (id) ON DELETE CASCADE
);

CREATE TABLE gccs.system_boundary_evidence (
    system_boundary_id uuid NOT NULL,
    evidence_item_id uuid NOT NULL,
    CONSTRAINT "PK_system_boundary_evidence" PRIMARY KEY (system_boundary_id, evidence_item_id),
    CONSTRAINT "FK_system_boundary_evidence_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
    CONSTRAINT "FK_system_boundary_evidence_system_boundaries_system_boundary_~" FOREIGN KEY (system_boundary_id) REFERENCES gccs.system_boundaries (id) ON DELETE CASCADE
);

CREATE TABLE gccs.user_roles (
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    CONSTRAINT "PK_user_roles" PRIMARY KEY (user_id, role_id),
    CONSTRAINT "FK_user_roles_roles_role_id" FOREIGN KEY (role_id) REFERENCES gccs.roles (id) ON DELETE CASCADE,
    CONSTRAINT "FK_user_roles_users_user_id" FOREIGN KEY (user_id) REFERENCES gccs.users (id) ON DELETE CASCADE
);

CREATE TABLE gccs.evidence_vendors (
    evidence_item_id uuid NOT NULL,
    vendor_id uuid NOT NULL,
    CONSTRAINT "PK_evidence_vendors" PRIMARY KEY (evidence_item_id, vendor_id),
    CONSTRAINT "FK_evidence_vendors_evidence_items_evidence_item_id" FOREIGN KEY (evidence_item_id) REFERENCES gccs.evidence_items (id) ON DELETE CASCADE,
    CONSTRAINT "FK_evidence_vendors_vendors_vendor_id" FOREIGN KEY (vendor_id) REFERENCES gccs.vendors (id) ON DELETE CASCADE
);

CREATE TABLE gccs.system_boundary_external_service_providers (
    system_boundary_id uuid NOT NULL,
    vendor_id uuid NOT NULL,
    CONSTRAINT "PK_system_boundary_external_service_providers" PRIMARY KEY (system_boundary_id, vendor_id),
    CONSTRAINT "FK_system_boundary_external_service_providers_system_boundarie~" FOREIGN KEY (system_boundary_id) REFERENCES gccs.system_boundaries (id) ON DELETE CASCADE,
    CONSTRAINT "FK_system_boundary_external_service_providers_vendors_vendor_id" FOREIGN KEY (vendor_id) REFERENCES gccs.vendors (id) ON DELETE CASCADE
);

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

CREATE TABLE gccs.contract_clause_obligations (
    contract_clause_id uuid NOT NULL,
    obligation_id text NOT NULL,
    CONSTRAINT "PK_contract_clause_obligations" PRIMARY KEY (contract_clause_id, obligation_id),
    CONSTRAINT "FK_contract_clause_obligations_contract_clauses_contract_claus~" FOREIGN KEY (contract_clause_id) REFERENCES gccs.contract_clauses (id) ON DELETE CASCADE,
    CONSTRAINT "FK_contract_clause_obligations_obligations_obligation_id" FOREIGN KEY (obligation_id) REFERENCES gccs.obligations (id) ON DELETE RESTRICT
);

CREATE INDEX "IX_annual_affirmations_tenant_id_status_due_at" ON gccs.annual_affirmations (tenant_id, status, due_at);

CREATE INDEX "IX_assessments_created_at_updated_at" ON gccs.assessments (created_at, updated_at);

CREATE INDEX "IX_assessments_tenant_id_status_level" ON gccs.assessments (tenant_id, status, level);

CREATE INDEX "IX_assets_created_at_updated_at" ON gccs.assets (created_at, updated_at);

CREATE INDEX "IX_assets_tenant_id_system_boundary_id" ON gccs.assets (tenant_id, system_boundary_id);

CREATE INDEX "IX_audit_log_entries_tenant_id_entity_type_entity_id" ON gccs.audit_log_entries (tenant_id, entity_type, entity_id);

CREATE INDEX "IX_audit_log_entries_tenant_id_occurred_at" ON gccs.audit_log_entries (tenant_id, occurred_at);

CREATE UNIQUE INDEX "IX_clauses_source_number" ON gccs.clauses (source, number);

CREATE INDEX "IX_company_certifications_company_profile_id_type" ON gccs.company_certifications (company_profile_id, type);

CREATE INDEX "IX_company_locations_company_profile_id" ON gccs.company_locations (company_profile_id);

CREATE UNIQUE INDEX "IX_company_naics_codes_company_profile_id_code" ON gccs.company_naics_codes (company_profile_id, code);

CREATE INDEX "IX_company_profiles_created_at_updated_at" ON gccs.company_profiles (created_at, updated_at);

CREATE UNIQUE INDEX "IX_company_profiles_tenant_id" ON gccs.company_profiles (tenant_id);

CREATE INDEX "IX_company_profiles_uei" ON gccs.company_profiles (uei);

CREATE INDEX "IX_compliance_tasks_created_at_updated_at" ON gccs.compliance_tasks (created_at, updated_at);

CREATE INDEX "IX_compliance_tasks_tenant_id_contract_id" ON gccs.compliance_tasks (tenant_id, contract_id);

CREATE INDEX "IX_compliance_tasks_tenant_id_obligation_id" ON gccs.compliance_tasks (tenant_id, obligation_id);

CREATE INDEX "IX_compliance_tasks_tenant_id_status_due_at" ON gccs.compliance_tasks (tenant_id, status, due_at);

CREATE INDEX "IX_contract_clause_obligations_obligation_id" ON gccs.contract_clause_obligations (obligation_id);

CREATE INDEX "IX_contract_clauses_contract_id_clause_number" ON gccs.contract_clauses (contract_id, clause_number);

CREATE INDEX "IX_contract_deliverables_contract_id_due_at" ON gccs.contract_deliverables (contract_id, due_at);

CREATE INDEX "IX_contract_documents_contract_id_type" ON gccs.contract_documents (contract_id, type);

CREATE INDEX "IX_contract_reporting_deadlines_contract_id_due_at" ON gccs.contract_reporting_deadlines (contract_id, due_at);

CREATE INDEX "IX_contract_subcontractors_subcontractor_id" ON gccs.contract_subcontractors (subcontractor_id);

CREATE INDEX "IX_contracts_created_at_updated_at" ON gccs.contracts (created_at, updated_at);

CREATE UNIQUE INDEX "IX_contracts_tenant_id_contract_number" ON gccs.contracts (tenant_id, contract_number);

CREATE INDEX "IX_contracts_tenant_id_status" ON gccs.contracts (tenant_id, status);

CREATE INDEX "IX_control_assessments_control_id" ON gccs.control_assessments (control_id);

CREATE INDEX "IX_controls_framework_cmmc_level" ON gccs.controls (framework, cmmc_level);

CREATE INDEX "IX_employees_created_at_updated_at" ON gccs.employees (created_at, updated_at);

CREATE INDEX "IX_employees_tenant_id_email" ON gccs.employees (tenant_id, email);

CREATE UNIQUE INDEX "IX_employees_tenant_id_employee_number" ON gccs.employees (tenant_id, employee_number);

CREATE INDEX "IX_evidence_contracts_contract_id" ON gccs.evidence_contracts (contract_id);

CREATE INDEX "IX_evidence_controls_control_id" ON gccs.evidence_controls (control_id);

CREATE INDEX "IX_evidence_employees_employee_id" ON gccs.evidence_employees (employee_id);

CREATE INDEX "IX_evidence_items_created_at_updated_at" ON gccs.evidence_items (created_at, updated_at);

CREATE INDEX "IX_evidence_items_tenant_id_expires_at" ON gccs.evidence_items (tenant_id, expires_at);

CREATE INDEX "IX_evidence_items_tenant_id_status" ON gccs.evidence_items (tenant_id, status);

CREATE INDEX "IX_evidence_obligations_obligation_id" ON gccs.evidence_obligations (obligation_id);

CREATE INDEX "IX_evidence_vendors_vendor_id" ON gccs.evidence_vendors (vendor_id);

CREATE INDEX "IX_flow_down_clauses_subcontractor_id_clause_number" ON gccs.flow_down_clauses (subcontractor_id, clause_number);

CREATE INDEX "IX_flow_down_clauses_subcontractor_id_contract_id" ON gccs.flow_down_clauses (subcontractor_id, contract_id);

CREATE INDEX "IX_flow_down_clauses_contract_clause_id" ON gccs.flow_down_clauses (contract_clause_id);

CREATE INDEX "IX_flow_down_clauses_contract_id_clause_number" ON gccs.flow_down_clauses (contract_id, clause_number);

CREATE INDEX "IX_flow_down_clauses_created_at_updated_at" ON gccs.flow_down_clauses (created_at, updated_at);

CREATE INDEX "IX_flow_down_clauses_obligation_id" ON gccs.flow_down_clauses (obligation_id);

CREATE INDEX "IX_flow_down_clauses_signed_evidence_item_id" ON gccs.flow_down_clauses (signed_evidence_item_id);

CREATE INDEX "IX_labor_category_rates_wage_determination_id" ON gccs.labor_category_rates (wage_determination_id);

CREATE INDEX "IX_labor_classifications_created_at_updated_at" ON gccs.labor_classifications (created_at, updated_at);

CREATE INDEX "IX_labor_classifications_tenant_id_employee_id_contract_id" ON gccs.labor_classifications (tenant_id, employee_id, contract_id);

CREATE INDEX "IX_obligations_source" ON gccs.obligations (source);

CREATE INDEX "IX_payroll_records_created_at_updated_at" ON gccs.payroll_records (created_at, updated_at);

CREATE INDEX "IX_payroll_records_tenant_id_employee_id_period_start_period_e~" ON gccs.payroll_records (tenant_id, employee_id, period_start, period_end);

CREATE INDEX "IX_poam_evidence_evidence_item_id" ON gccs.poam_evidence (evidence_item_id);

CREATE INDEX "IX_poam_items_created_at_updated_at" ON gccs.poam_items (created_at, updated_at);

CREATE INDEX "IX_poam_items_tenant_id_status_target_completion_at" ON gccs.poam_items (tenant_id, status, target_completion_at);

CREATE INDEX "IX_report_contracts_report_entity_id" ON gccs.report_contracts (report_entity_id);

CREATE INDEX "IX_report_evidence_report_entity_id" ON gccs.report_evidence (report_entity_id);

CREATE INDEX "IX_report_obligations_report_entity_id" ON gccs.report_obligations (report_entity_id);

CREATE INDEX "IX_reports_created_at_updated_at" ON gccs.reports (created_at, updated_at);

CREATE INDEX "IX_reports_tenant_id_type_status" ON gccs.reports (tenant_id, type, status);

CREATE INDEX "IX_roles_created_at_updated_at" ON gccs.roles (created_at, updated_at);

CREATE UNIQUE INDEX "IX_roles_tenant_id_name" ON gccs.roles (tenant_id, name);

CREATE INDEX "IX_solicitations_created_at_updated_at" ON gccs.solicitations (created_at, updated_at);

CREATE UNIQUE INDEX "IX_solicitations_tenant_id_solicitation_number" ON gccs.solicitations (tenant_id, solicitation_number);

CREATE INDEX "IX_subcontractor_evidence_evidence_item_id" ON gccs.subcontractor_evidence (evidence_item_id);

CREATE INDEX "IX_subcontractor_evidence_requests_created_at_updated_at" ON gccs.subcontractor_evidence_requests (created_at, updated_at);

CREATE INDEX "IX_subcontractor_evidence_requests_obligation_id" ON gccs.subcontractor_evidence_requests (obligation_id);

CREATE INDEX "IX_subcontractor_evidence_requests_received_evidence_item_id" ON gccs.subcontractor_evidence_requests (received_evidence_item_id);

CREATE INDEX "IX_subcontractor_evidence_requests_related_flow_down_clause_id" ON gccs.subcontractor_evidence_requests (related_flow_down_clause_id);

CREATE INDEX "IX_subcontractor_evidence_requests_subcontractor_id_due_date" ON gccs.subcontractor_evidence_requests (subcontractor_id, due_date);

CREATE INDEX "IX_subcontractor_evidence_requests_tenant_id_status_due_date" ON gccs.subcontractor_evidence_requests (tenant_id, status, due_date);

CREATE INDEX "IX_subcontractors_created_at_updated_at" ON gccs.subcontractors (created_at, updated_at);

CREATE INDEX "IX_subcontractors_tenant_id_name" ON gccs.subcontractors (tenant_id, name);

CREATE INDEX "IX_subcontractors_tenant_id_uei" ON gccs.subcontractors (tenant_id, uei);

CREATE INDEX "IX_system_boundaries_created_at_updated_at" ON gccs.system_boundaries (created_at, updated_at);

CREATE INDEX "IX_system_boundaries_tenant_id_status" ON gccs.system_boundaries (tenant_id, status);

CREATE INDEX "IX_system_boundary_assets_asset_id" ON gccs.system_boundary_assets (asset_id);

CREATE INDEX "IX_system_boundary_evidence_evidence_item_id" ON gccs.system_boundary_evidence (evidence_item_id);

CREATE INDEX "IX_system_boundary_external_service_providers_vendor_id" ON gccs.system_boundary_external_service_providers (vendor_id);

CREATE INDEX "IX_tenants_created_at_updated_at" ON gccs.tenants (created_at, updated_at);

CREATE INDEX "IX_training_records_created_at_updated_at" ON gccs.training_records (created_at, updated_at);

CREATE INDEX "IX_training_records_tenant_id_employee_id_status" ON gccs.training_records (tenant_id, employee_id, status);

CREATE INDEX "IX_training_records_tenant_id_expires_at" ON gccs.training_records (tenant_id, expires_at);

CREATE INDEX "IX_user_roles_role_id" ON gccs.user_roles (role_id);

CREATE INDEX "IX_users_created_at_updated_at" ON gccs.users (created_at, updated_at);

CREATE UNIQUE INDEX "IX_users_tenant_id_email" ON gccs.users (tenant_id, email);

CREATE INDEX "IX_vendors_created_at_updated_at" ON gccs.vendors (created_at, updated_at);

CREATE INDEX "IX_vendors_tenant_id_type" ON gccs.vendors (tenant_id, type);

CREATE INDEX "IX_wage_determinations_created_at_updated_at" ON gccs.wage_determinations (created_at, updated_at);

CREATE UNIQUE INDEX "IX_wage_determinations_tenant_id_determination_number_revision" ON gccs.wage_determinations (tenant_id, determination_number, revision);

INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260610031239_InitialDevelopmentSchema', '10.0.4');

COMMIT;

START TRANSACTION;
ALTER TABLE gccs.contract_clauses ADD review_state character varying(64) NOT NULL DEFAULT 'Draft';

ALTER TABLE gccs.contract_clauses ADD source_hash character varying(128);

ALTER TABLE gccs.clauses ADD clause_effective_at date;

ALTER TABLE gccs.clauses ADD clause_text_version character varying(120) NOT NULL DEFAULT 'current';

ALTER TABLE gccs.clauses ADD review_state character varying(64) NOT NULL DEFAULT 'Draft';

ALTER TABLE gccs.clauses ADD source_hash character varying(128);

ALTER TABLE gccs.clauses ADD superseded_at date;

ALTER TABLE gccs.clauses ADD superseded_by_clause_id character varying(120);

INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260610051044_AddClauseReviewVersioning', '10.0.4');

COMMIT;

START TRANSACTION;
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

CREATE INDEX "IX_tenant_memberships_created_at_updated_at" ON gccs.tenant_memberships (created_at, updated_at);

CREATE INDEX "IX_tenant_memberships_tenant_id_status" ON gccs.tenant_memberships (tenant_id, status);

CREATE UNIQUE INDEX "IX_tenant_memberships_tenant_id_user_id" ON gccs.tenant_memberships (tenant_id, user_id);

CREATE INDEX "IX_tenant_memberships_user_id" ON gccs.tenant_memberships (user_id);

INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260613213418_AddTenantMemberships', '10.0.4');

COMMIT;

START TRANSACTION;
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

CREATE INDEX "IX_tenant_invitations_created_at_updated_at" ON gccs.tenant_invitations (created_at, updated_at);

CREATE UNIQUE INDEX "IX_tenant_invitations_invitation_token" ON gccs.tenant_invitations (invitation_token);

CREATE INDEX "IX_tenant_invitations_tenant_id_email_status" ON gccs.tenant_invitations (tenant_id, email, status);

CREATE INDEX "IX_tenant_invitations_tenant_id_status_expires_at" ON gccs.tenant_invitations (tenant_id, status, expires_at);

INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260613221118_AddTenantInvitations', '10.0.4');

COMMIT;

START TRANSACTION;
ALTER TABLE gccs.audit_log_entries DROP CONSTRAINT "FK_audit_log_entries_tenants_tenant_id";

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

CREATE INDEX "IX_no_cui_acknowledgements_created_at_updated_at" ON gccs.no_cui_acknowledgements (created_at, updated_at);

CREATE UNIQUE INDEX "IX_no_cui_acknowledgements_tenant_id_user_id_notice_version" ON gccs.no_cui_acknowledgements (tenant_id, user_id, notice_version);

INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260615003848_AddNoCuiAcknowledgements', '10.0.4');

COMMIT;

START TRANSACTION;
ALTER TABLE gccs.evidence_items ADD content_type character varying(160);

ALTER TABLE gccs.evidence_items ADD malware_scan_status character varying(80);

ALTER TABLE gccs.evidence_items ADD original_file_name character varying(240);

ALTER TABLE gccs.evidence_items ADD size_bytes bigint;

ALTER TABLE gccs.evidence_items ADD upload_validation_status character varying(80);

INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260615005659_AddEvidenceUploadGuardrails', '10.0.4');

COMMIT;

START TRANSACTION;
ALTER TABLE gccs.audit_log_entries ADD correlation_id character varying(120) NOT NULL DEFAULT '';

INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260615010139_AddAuditRequestMetadata', '10.0.4');

COMMIT;

START TRANSACTION;
ALTER TABLE gccs.obligations ADD confidence text NOT NULL DEFAULT 'unknown';

ALTER TABLE gccs.obligations ADD last_reviewed_at date NOT NULL DEFAULT DATE '-infinity';

ALTER TABLE gccs.obligations ADD next_review_due_at date;

ALTER TABLE gccs.obligations ADD requires_expert_review boolean NOT NULL DEFAULT FALSE;

ALTER TABLE gccs.obligations ADD requires_flow_down boolean NOT NULL DEFAULT FALSE;

ALTER TABLE gccs.obligations ADD review_state character varying(64) NOT NULL DEFAULT 'Draft';

ALTER TABLE gccs.obligations ADD reviewed_by_user_id uuid;

INSERT INTO gccs."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260615011257_AddObligationPublicationMetadata', '10.0.4');

COMMIT;
