import { useEffect, useState } from "react";

const hubSpotScriptId = "hs-script-loader";
const hubSpotScriptUrl = "https://js-na2.hs-scripts.com/247124975.js";
const preferenceStorageKey = "fedril.hubspot-chat-consent.v1";

type ChatPreference = "accepted" | "declined" | null;

type HubSpotWidget = {
  refresh: () => void;
  remove: () => void;
};

declare global {
  interface Window {
    HubSpotConversations?: { widget: HubSpotWidget };
    hsConversationsOnReady?: Array<() => void>;
  }
}

function readPreference(): ChatPreference {
  try {
    const value = window.localStorage.getItem(preferenceStorageKey);
    return value === "accepted" || value === "declined" ? value : null;
  } catch {
    return null;
  }
}

function writePreference(preference: Exclude<ChatPreference, null>) {
  try {
    window.localStorage.setItem(preferenceStorageKey, preference);
  } catch {
    // Storage can be unavailable in privacy modes. Consent still applies to this page load.
  }
}

function removeHubSpotChat() {
  window.HubSpotConversations?.widget.remove();
  document.getElementById(hubSpotScriptId)?.remove();
}

export function HubSpotChat() {
  const [preference, setPreference] = useState<ChatPreference>(readPreference);
  const [isChoosing, setIsChoosing] = useState(preference === null);

  useEffect(() => {
    if (preference !== "accepted") {
      return;
    }

    let hasRefreshed = false;
    const refreshWidget = () => {
      const widget = window.HubSpotConversations?.widget;
      if (!widget || hasRefreshed) {
        return;
      }
      hasRefreshed = true;
      widget.refresh();
    };
    const readyCallbacks = window.hsConversationsOnReady ?? [];
    readyCallbacks.push(refreshWidget);
    window.hsConversationsOnReady = readyCallbacks;

    const existingScript = document.getElementById(hubSpotScriptId);
    if (existingScript) {
      if (window.HubSpotConversations) {
        refreshWidget();
      } else {
        existingScript.addEventListener("load", refreshWidget, { once: true });
      }
    } else {
      const script = document.createElement("script");
      script.id = hubSpotScriptId;
      script.src = hubSpotScriptUrl;
      script.async = true;
      script.defer = true;
      script.addEventListener("load", refreshWidget, { once: true });
      document.body.append(script);
    }

    return () => {
      const callbackIndex = window.hsConversationsOnReady?.indexOf(refreshWidget) ?? -1;
      if (callbackIndex >= 0) {
        window.hsConversationsOnReady?.splice(callbackIndex, 1);
      }
      document.getElementById(hubSpotScriptId)?.removeEventListener("load", refreshWidget);
    };
  }, [preference]);

  const acceptChat = () => {
    writePreference("accepted");
    setPreference("accepted");
    setIsChoosing(false);
  };

  const declineChat = () => {
    writePreference("declined");
    removeHubSpotChat();
    setPreference("declined");
    setIsChoosing(false);
  };

  const disableChat = () => {
    writePreference("declined");
    removeHubSpotChat();
    window.location.reload();
  };

  if (isChoosing) {
    return (
      <aside className="hubspot-consent" aria-label="HubSpot chat privacy choices">
        <div>
          <strong>Chat privacy</strong>
          <p>
            Enable HubSpot chat to contact FeDril from this public page. HubSpot will receive page and
            interaction data and may use cookies. Do not share CUI, FCI, classified information,
            credentials, or other sensitive content in chat.
          </p>
        </div>
        <div className="hubspot-consent__actions">
          <button className="hubspot-consent__accept" onClick={acceptChat} type="button">
            Enable chat
          </button>
          <button className="hubspot-consent__decline" onClick={declineChat} type="button">
            Not now
          </button>
        </div>
      </aside>
    );
  }

  return (
    <button
      className="hubspot-consent-settings"
      onClick={preference === "accepted" ? disableChat : () => setIsChoosing(true)}
      type="button"
    >
      {preference === "accepted" ? "Disable HubSpot chat" : "Chat privacy settings"}
    </button>
  );
}
