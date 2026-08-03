import {fileURLToPath} from "node:url";
import {resolve} from "node:path";

type JsonRecord = Record<string, unknown>;

const tenantId = "11111111-1111-1111-1111-111111111113";
const managerUserId = "22222222-2222-2222-2222-222222222243";
const evidenceItemId = "cccccccc-cccc-cccc-cccc-ccccccccccc2";
const contractNumber = "NPS-DEMO-2026-001";
const apiBaseUrl = new URL(process.env.FEDRIL_DEMO_API_URL ?? "http://127.0.0.1:5064");
const permittedHosts = new Set(["127.0.0.1", "localhost"]);

if (apiBaseUrl.protocol !== "http:" || !permittedHosts.has(apiBaseUrl.hostname)) {
  throw new Error("FeDril demo setup refused a non-loopback API URL.");
}

const privateHeaders = {
  "Content-Type": "application/json",
  "X-Gccs-Dev-Auth": "true",
  "X-Gccs-Dev-Tenant": tenantId,
  "X-Gccs-Tenant": tenantId,
  "X-Gccs-Dev-User": managerUserId,
  "X-Gccs-Dev-Email": "priya.shah.northstar@example.com",
  "X-Gccs-Dev-Role": "ComplianceManager"
};

export async function seedDemo() {
  await assertMarketingDemoApi();
  await requestJson("/api/dev/compliance-content/import", {method: "POST"}, "import source-backed compliance content");

  const contracts = await requestJson<JsonRecord[]>("/api/contracts", {}, "list fictional contracts");
  let contract = contracts.find((candidate) => candidate.contractNumber === contractNumber);
  if (!contract) {
    contract = await requestJson<JsonRecord>(
      "/api/contracts",
      {
        method: "POST",
        body: JSON.stringify({
          contractNumber,
          title: "Precision machining readiness exercise",
          agencyOrPrimeName: "Orion Aeronautics Group (fictional)",
          relationship: "Prime",
          kind: "FixedPrice",
          status: "Active",
          awardedAt: "2026-01-05",
          periodOfPerformanceStart: "2026-01-15",
          periodOfPerformanceEnd: "2027-12-31",
          placeOfPerformance: "Columbus, Ohio",
          description: "Fictional, non-sensitive record created only for the FeDril marketing demonstration. No customer, CUI, or FCI data is stored.",
          dataHandlingPosture: "NoFciOrCui"
        })
      },
      "create the fictional contract"
    );
  }

  const contractId = requiredString(contract, "id", "fictional contract");
  const clauseIds = ["far-52-204-21", "far-52-204-25"];
  const existingClauses = await requestJson<JsonRecord[]>(
    `/api/contracts/${contractId}/clauses`,
    {},
    "list fictional contract clauses"
  );

  const associatedClauses: Array<{clauseLibraryId: string; contractClauseId: string}> = [];
  for (const clauseLibraryId of clauseIds) {
    let clause = existingClauses.find((candidate) => candidate.clauseLibraryId === clauseLibraryId);
    if (!clause) {
      clause = await requestJson<JsonRecord>(
        `/api/contracts/${contractId}/clauses`,
        {
          method: "POST",
          body: JSON.stringify({
            clauseLibraryId,
            attachmentReason: "Fictional readiness scenario using a published, source-backed clause record.",
            sourceDocumentReference: "Northstar fictional demonstration scenario"
          })
        },
        "associate a source-backed clause"
      );
      existingClauses.push(clause);
    }

    const contractClauseId = requiredString(clause, "id", "associated clause");
    associatedClauses.push({clauseLibraryId, contractClauseId});
  }

  let obligations = await requestJson<JsonRecord[]>(
    `/api/contract-obligations?contractId=${encodeURIComponent(contractId)}`,
    {},
    "load readiness obligations"
  );
  let repairedMissingObligation = false;
  for (const {clauseLibraryId, contractClauseId} of associatedClauses) {
    const obligationExists = obligations.some(
      (candidate) => candidate.obligationId === clauseLibraryId && candidate.contractClauseId === contractClauseId
    );
    if (!obligationExists) {
      await requestJson(
        `/api/contracts/${contractId}/clauses/${contractClauseId}/obligations/generate`,
        {method: "POST"},
        "repair missing source-backed readiness work"
      );
      repairedMissingObligation = true;
    }
  }
  if (repairedMissingObligation) {
    obligations = await requestJson<JsonRecord[]>(
      `/api/contract-obligations?contractId=${encodeURIComponent(contractId)}`,
      {},
      "reload repaired readiness obligations"
    );
  }
  const primary = findObligation(obligations, "far-52-204-21");
  const completed = findObligation(obligations, "far-52-204-25");

  if (primary.status !== "InProgress") {
    await requestJson(
      `/api/contract-obligations/${requiredString(primary, "contractClauseId", "primary obligation")}/far-52-204-21/status`,
      {method: "PATCH", body: JSON.stringify({status: "InProgress"})},
      "set the primary readiness status"
    );
  }
  if (completed.status !== "Done") {
    await requestJson(
      `/api/contract-obligations/${requiredString(completed, "contractClauseId", "completed obligation")}/far-52-204-25/status`,
      {method: "PATCH", body: JSON.stringify({status: "Done"})},
      "set the completed readiness status"
    );
  }

  const primaryContractClauseId = requiredString(primary, "contractClauseId", "primary obligation");
  const primaryDetail = await requestJson<JsonRecord>(
    `/api/contract-obligations/${primaryContractClauseId}/far-52-204-21`,
    {},
    "load the primary readiness detail"
  );
  const linkedTasks = Array.isArray(primaryDetail.linkedTasks) ? primaryDetail.linkedTasks as JsonRecord[] : [];
  const remediationTask = linkedTasks[0];
  if (!remediationTask) {
    throw new Error("FeDril demo setup could not find the server-linked remediation task.");
  }
  if (
    remediationTask.title !== "Complete access-control evidence review" ||
    remediationTask.dueAt !== "2026-07-24" ||
    remediationTask.riskLevel !== "High"
  ) {
    await requestJson(
      `/api/tasks/${requiredString(remediationTask, "id", "remediation task")}`,
      {
        method: "PATCH",
        body: JSON.stringify({
          title: "Complete access-control evidence review",
          description: "Review the fictional quarterly access summary and document the remediation decision.",
          priority: "High",
          dueAt: "2026-07-24"
        })
      },
      "set the remediation priority and due date"
    );
  }

  const evidenceItems = await requestJson<JsonRecord[]>("/api/evidence-items", {}, "load fictional evidence metadata");
  const evidence = evidenceItems.find((candidate) => candidate.id === evidenceItemId);
  if (!evidence) {
    throw new Error("FeDril demo setup could not find the reserved fictional evidence metadata record.");
  }
  const obligationIds = Array.isArray(evidence.obligationIds) ? evidence.obligationIds : [];
  if (!obligationIds.includes("far-52-204-21") || evidence.type !== "AccessReview") {
    await requestJson(
      `/api/evidence-items/${evidenceItemId}`,
      {
        method: "PUT",
        body: JSON.stringify({
          title: "Northstar quarterly access review summary",
          type: "AccessReview",
          ownerFunction: "Security",
          status: "Approved",
          effectiveAt: "2026-06-15",
          expiresAt: "2027-06-15",
          tags: ["access-review", "fictional-demo", "no-cui", "northstar"],
          description: "Fictional, non-sensitive evidence metadata. No file content is stored.",
          obligationIds: ["far-52-204-21"],
          controlIds: [],
          contractIds: [contractId],
          vendorIds: [],
          subcontractorIds: [],
          employeeIds: [],
          reportIds: [],
          classification: {
            classification: "Unclassified",
            source: "UserSelected",
            confidence: 1,
            reason: "Fictional, non-sensitive No-CUI marketing demonstration metadata.",
            isApprovedDemoContent: true
          }
        })
      },
      "associate fictional evidence metadata"
    );
  }

  const reports = await requestJson<JsonRecord[]>("/api/reports/recent?limit=25", {}, "load readiness summaries");
  if (!reports.some((report) => report.type === "ComplianceStatus")) {
    await requestJson("/api/reports/compliance-status", {method: "POST"}, "generate the leadership readiness summary");
  }

  obligations = await requestJson<JsonRecord[]>(
    `/api/contract-obligations?contractId=${encodeURIComponent(contractId)}`,
    {},
    "verify readiness obligations"
  );
  const verifiedPrimary = findObligation(obligations, "far-52-204-21");
  if (verifiedPrimary.dueAt !== "2026-07-24" || verifiedPrimary.riskLevel !== "High") {
    throw new Error("FeDril demo setup did not produce the expected high-priority overdue readiness item.");
  }

  process.stdout.write("FeDril fictional demo data verified for Northstar Precision Systems.\n");
}

async function assertMarketingDemoApi() {
  const context = await requestJson<JsonRecord>(
    "/api/development/testing-context",
    {},
    "verify the Development marketing-demo API marker"
  );
  const tenants = Array.isArray(context.tenants) ? context.tenants as JsonRecord[] : [];
  const personas = Array.isArray(context.personas) ? context.personas as JsonRecord[] : [];
  const northstar = tenants.find((tenant) => tenant.tenantId === tenantId);
  const manager = personas.find((persona) => persona.tenantId === tenantId && persona.userId === managerUserId);
  if (
    northstar?.displayName !== "Northstar Precision Systems" ||
    northstar.dataHandlingMode !== "NoCui" ||
    northstar.isSelectable !== true ||
    manager?.email !== "priya.shah.northstar@example.com" ||
    manager.roleName !== "Compliance Manager"
  ) {
    throw new Error("FeDril demo setup refused an API without the expected Development-only Northstar marker.");
  }
}

async function requestJson<T = JsonRecord>(path: string, init: RequestInit, operation: string): Promise<T> {
  const url = new URL(path, apiBaseUrl);
  const response = await fetch(url, {
    ...init,
    headers: privateHeaders,
    signal: AbortSignal.timeout(30_000)
  });
  if (!response.ok) {
    throw new Error(`FeDril demo setup could not ${operation} (HTTP ${response.status}).`);
  }
  return response.json() as Promise<T>;
}

function requiredString(record: JsonRecord, key: string, label: string) {
  const value = record[key];
  if (typeof value !== "string" || value.length === 0) {
    throw new Error(`FeDril demo setup received an invalid ${label}.`);
  }
  return value;
}

function findObligation(records: JsonRecord[], obligationId: string) {
  const item = records.find((candidate) => candidate.obligationId === obligationId);
  if (!item) {
    throw new Error("FeDril demo setup could not find an expected source-backed readiness item.");
  }
  return item;
}

const invokedPath = process.argv[1] ? resolve(process.argv[1]) : "";
if (invokedPath === fileURLToPath(import.meta.url)) {
  await seedDemo();
}
