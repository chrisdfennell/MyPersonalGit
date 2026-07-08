// Paste/drop image upload for comment textareas.
// Any <textarea data-attach-upload> gets support for pasting or dropping images:
// the image is uploaded via the registered .NET handler and a markdown image
// reference is inserted at the cursor position.
(function () {
    let dotnetRef = null;

    function isUploadTarget(el) {
        return el && el.tagName === 'TEXTAREA' && el.hasAttribute('data-attach-upload');
    }

    function extractImageFiles(items) {
        const files = [];
        if (!items) return files;
        for (const item of items) {
            // DataTransferItemList (paste) or FileList (drop)
            const file = typeof item.getAsFile === 'function' ? item.getAsFile() : item;
            if (file && file.type && file.type.startsWith('image/')) {
                files.push(file);
            }
        }
        return files;
    }

    function insertAtCursor(textarea, text) {
        const start = textarea.selectionStart ?? textarea.value.length;
        const end = textarea.selectionEnd ?? textarea.value.length;
        textarea.value = textarea.value.slice(0, start) + text + textarea.value.slice(end);
        const pos = start + text.length;
        textarea.selectionStart = textarea.selectionEnd = pos;
        notifyBlazor(textarea);
    }

    function replaceText(textarea, oldText, newText) {
        const idx = textarea.value.indexOf(oldText);
        if (idx === -1) {
            insertAtCursor(textarea, newText);
            return;
        }
        textarea.value = textarea.value.slice(0, idx) + newText + textarea.value.slice(idx + oldText.length);
        textarea.selectionStart = textarea.selectionEnd = idx + newText.length;
        notifyBlazor(textarea);
    }

    function notifyBlazor(textarea) {
        // Fire both events so it works with @bind (change) and @bind:event="oninput"
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
        textarea.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function readAsBase64(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result.split(',', 2)[1]);
            reader.onerror = () => reject(reader.error);
            reader.readAsDataURL(file);
        });
    }

    let uploadCounter = 0;

    async function uploadFiles(textarea, files) {
        if (!dotnetRef) return;
        for (const file of files) {
            const name = file.name || 'image.png';
            const placeholder = `![Uploading ${name}…(${++uploadCounter})]()`;
            insertAtCursor(textarea, placeholder);
            try {
                const base64 = await readAsBase64(file);
                const url = await dotnetRef.invokeMethodAsync('UploadPastedImage', name, file.type, base64);
                replaceText(textarea, placeholder, url ? `![${name}](${url})` : '');
                if (!url) {
                    console.warn('commentAttachments: upload rejected (type or size limit)');
                }
            } catch (err) {
                console.error('commentAttachments: upload failed', err);
                replaceText(textarea, placeholder, '');
            }
        }
    }

    function onPaste(e) {
        if (!isUploadTarget(e.target)) return;
        const files = extractImageFiles(e.clipboardData && e.clipboardData.items);
        if (files.length === 0) return;
        e.preventDefault();
        uploadFiles(e.target, files);
    }

    function onDrop(e) {
        if (!isUploadTarget(e.target)) return;
        const files = extractImageFiles(e.dataTransfer && e.dataTransfer.files);
        if (files.length === 0) return;
        e.preventDefault();
        uploadFiles(e.target, files);
    }

    function onDragOver(e) {
        if (isUploadTarget(e.target)) e.preventDefault();
    }

    window.commentAttachments = {
        init: function (ref) {
            dotnetRef = ref;
        },
        dispose: function () {
            dotnetRef = null;
        }
    };

    document.addEventListener('paste', onPaste, true);
    document.addEventListener('drop', onDrop, true);
    document.addEventListener('dragover', onDragOver, true);
})();
