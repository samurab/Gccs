export type DemoRequestSubmission = {
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  company: string;
  referralSource: string | null;
  employeeCount: string | null;
  message: string | null;
  preferredStartAt: string;
  preferredTimeZone: string;
  privacyConsent: boolean;
  website: string | null;
};

export type DemoRequestReceipt = {
  status: "Received";
  receivedAt: string;
};

export type DemoRequestResult =
  | { data: DemoRequestReceipt; error: null; fieldErrors: null }
  | { data: null; error: string; fieldErrors: Record<string, string[]> };

export type DemoFollowUpContext = {
  status: "Pending" | "Responded" | "Expired" | string;
  requestedAt: string;
  expiresAt: string;
  noCuiNoticeVersion: string;
};

export type DemoFollowUpSubmission = {
  token: string;
  workflows: string[];
  otherWorkflow: string | null;
  goals: string;
  challenges: string;
  currentProcess: string | null;
  additionalContext: string | null;
  noCuiConfirmed: boolean;
  website: string | null;
};

export type DemoFollowUpResult<T> =
  | { data: T; error: null; fieldErrors: null }
  | { data: null; error: string; fieldErrors: Record<string, string[]> };

export async function submitDemoRequest(request: DemoRequestSubmission): Promise<DemoRequestResult> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";
  try {
    const response = await fetch(`${apiBaseUrl}/api/public/demo-requests`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string; title?: string; errors?: Record<string, string[]> } | null;
      return {
        data: null,
        error: problem?.detail || problem?.title || "The request could not be submitted.",
        fieldErrors: problem?.errors ?? {},
      };
    }
    return { data: await response.json() as DemoRequestReceipt, error: null, fieldErrors: null };
  } catch {
    return { data: null, error: "The demo-request service could not be reached. Check your connection and try again.", fieldErrors: {} };
  }
}

async function postDemoFollowUp<T>(path: string, body: unknown): Promise<DemoFollowUpResult<T>> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as {
        detail?: string;
        title?: string;
        errors?: Record<string, string[]>;
      } | null;
      return {
        data: null,
        error: problem?.detail || problem?.title || "The demo follow-up request could not be completed.",
        fieldErrors: problem?.errors ?? {},
      };
    }

    return { data: await response.json() as T, error: null, fieldErrors: null };
  } catch {
    return { data: null, error: "The demo follow-up service could not be reached. Check your connection and try again.", fieldErrors: {} };
  }
}

export function getDemoFollowUpContext(token: string): Promise<DemoFollowUpResult<DemoFollowUpContext>> {
  return postDemoFollowUp<DemoFollowUpContext>("/api/public/demo-request-details/context", { token });
}

export function submitDemoFollowUpResponse(
  request: DemoFollowUpSubmission
): Promise<DemoFollowUpResult<{ status: "Received"; submittedAt: string }>> {
  return postDemoFollowUp<{ status: "Received"; submittedAt: string }>(
    "/api/public/demo-request-details/responses",
    request
  );
}
