// Set up event handlers
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const retryButton = document.getElementById("components-reconnect-button");
retryButton.addEventListener("click", retry);

const resumeButton = document.getElementById("components-resume-button");
resumeButton.addEventListener("click", resume);

function applyThemeToDialog() {
    var isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
    if (isDark) {
        reconnectModal.style.setProperty('background-color', '#1e1e2e', 'important');
        reconnectModal.style.setProperty('color', '#c9d1d9', 'important');
        reconnectModal.style.setProperty('border-color', '#444', 'important');
        // Also style child paragraphs
        reconnectModal.querySelectorAll('p').forEach(function(p) {
            p.style.setProperty('color', '#8b949e', 'important');
        });
        // Style the animation rings
        reconnectModal.querySelectorAll('.components-rejoining-animation div').forEach(function(d) {
            d.style.setProperty('border-color', '#58a6ff', 'important');
        });
    } else {
        reconnectModal.style.backgroundColor = '';
        reconnectModal.style.color = '';
        reconnectModal.style.borderColor = '';
    }
}

function handleReconnectStateChanged(event) {
    if (event.detail.state === "show") {
        applyThemeToDialog();
        reconnectModal.showModal();
    } else if (event.detail.state === "hide") {
        reconnectModal.close();
    } else if (event.detail.state === "failed") {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (event.detail.state === "rejected") {
        location.reload();
    }
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

    try {
        // Reconnect will asynchronously return:
        // - true to mean success
        // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
        // - exception to mean we didn't reach the server (this can be sync or async)
        const successful = await Blazor.reconnect();
        if (!successful) {
            // We have been able to reach the server, but the circuit is no longer available.
            // We'll reload the page so the user can continue using the app as quickly as possible.
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                location.reload();
            } else {
                reconnectModal.close();
            }
        }
    } catch (err) {
        // We got an exception, server is currently unavailable
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            location.reload();
        }
    } catch {
        reconnectModal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}
