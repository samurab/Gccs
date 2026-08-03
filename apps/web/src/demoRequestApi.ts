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
  | { data: DemoRequestReceipt; error: null }
  | { data: null; error: string };

export async function submitDemoRequest(request: DemoRequestSubmission): Promise<DemoRequestResult> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";
  try {
    const response = await fetch(`${apiBaseUrl}/api/public/demo-requests`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
      return { data: null, error: problem?.detail || problem?.title || "The request could not be submitted." };
    }
    return { data: await response.json() as DemoRequestReceipt, error: null };
  } catch {
    return { data: null, error: "The demo-request service could not be reached. Check your connection and try again." };
  }
}
