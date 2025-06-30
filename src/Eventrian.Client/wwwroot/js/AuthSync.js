const channel = new BroadcastChannel("auth-channel");
let currentUserId = null;
let listenerInitialized = false;

// Called after login
export function setCurrentUserId(userId) {
    currentUserId = userId;
}

// Called after logout
export function clearCurrentUserId() {
    currentUserId = null;
}

// Broadcasts logout to other tabs
export function broadcastLogout(userId) {
    console.log("[Broadcast] Sending logout for:", userId);
    channel.postMessage({ type: "logout", userId });
}

// Sets up listener for logout messages from other tabs
export function initLogoutSync(userId, dotnetHelper) {
    currentUserId = userId;

    if (listenerInitialized) return;
    listenerInitialized = true;

    const channel = new BroadcastChannel("auth-channel");

    console.log("[Broadcast] Listening for:", currentUserId);

    channel.onmessage = (event) => {
        if (event.data?.type === "logout") {
            const incomingUserId = event.data.userId;
            console.log("[Broadcast] Received logout for:", incomingUserId);

            if (incomingUserId === currentUserId) {
                console.log("[Broadcast] Match — logging out");
                dotnetHelper.invokeMethodAsync("OnBroadcastLogoutMatch");
            }
        }
    };
}

// Optional: re-init on tab focus (if ever needed again)
window.addEventListener("focus", () => {
    if (!listenerInitialized && currentUserId && window._dotnetHelper) {
        console.log("[Broadcast] Reinitializing listener on focus");
        initLogoutListener(window._dotnetHelper);
    }
});
