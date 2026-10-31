# HubSpot synchronization for Book-a-Demo requests

The public `POST /api/public/demo-requests` endpoint persists the request and three delivery records in one database transaction when HubSpot synchronization is enabled:

- internal operations notification;
- requester acknowledgement; and
- `HubSpotSync`.

The existing demo-request worker claims and retries the CRM delivery. The public request does not wait on HubSpot, and a temporary HubSpot failure does not lose the submitted request.

## CRM behavior

- Contacts are matched by normalized email and created or updated without duplicates.
- Companies are matched by email domain and associated to the contact. Common personal-email domains do not create companies because a supplied company name is not a safe unique key.
- A new contact is classified with acquisition source `Book a Demo`, relationship status `Demo Interest`, interest level `High`, and prospecting status `Meeting Requested`.
- The next action directs an operator to review the requested time and confirm it or offer an alternative. The follow-up date is the next UTC business date.
- Form consent authorizes manual handling of the request; it does not imply marketing consent. New records therefore use outreach permission `Manual Only`.
- Existing acquisition and outreach-permission values are preserved. Customer, partner, do-not-contact, scheduled-meeting, converted-opportunity, and other advanced states are not downgraded.
- The FeDril source-detail field records the demo-request identifier and the submitted referral source. Retries are idempotent.
- No Google or Microsoft calendar event is created. The submitted time remains a request until an operator uses the existing confirmation workflow.

## Runtime configuration

Use these application settings only in the production environment that should write to the production CRM:

```text
DemoRequests__HubSpot__Enabled=true
DemoRequests__HubSpot__BaseUrl=https://api.hubapi.com
DemoRequests__HubSpot__PrivateAppToken=<secret>
```

The private app needs contact and company read/write scopes. Store its token in the deployment secret store; never commit it. The API fails startup when synchronization is enabled without a valid HTTPS base URL and token.

Staging keeps `DemoRequests__HubSpot__Enabled=false` unless it is later connected to a separate HubSpot test account.
