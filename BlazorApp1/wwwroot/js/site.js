function setSignature(element, color, image) {

    var canvas = document.getElementById(element.id)
    var signaturePad = new SignaturePad(canvas, {
        backgroundColor: 'rgba(255, 255, 255,00)',
        penColor: color
    });
    //signaturePad.penColor = color;
    if (image != null) {
        signaturePad.fromDataURL(image);
    }
    document.getElementById('clear-signature').addEventListener('click', function () {
        signaturePad.clear();

    });

    document.getElementById('red').addEventListener('click', function () {
        signaturePad.penColor = "rgba(255,0,0,0.9)";
        signaturePad.minWidth = 6;
        signaturePad.maxWidth = 6;
    });
    document.getElementById('yellowsmall').addEventListener('click', function () {
        signaturePad.penColor = "rgba(255,255,0,0.8)";
        signaturePad.minWidth = 7;
        signaturePad.maxWidth = 7;
    });
    document.getElementById('yellowbig').addEventListener('click', function () {
        signaturePad.penColor = "rgba(255,255,0,0.7)";
        signaturePad.minWidth = 12;
        signaturePad.maxWidth = 12;
    });
    document.getElementById('orange').addEventListener('click', function () {
        signaturePad.penColor = "rgba(255,165,0,0.8)";
        signaturePad.minWidth = 7;
        signaturePad.maxWidth = 7;
    });
    document.getElementById('purple').addEventListener('click', function () {
        signaturePad.penColor = "rgba(128,0,128,0.8)";
        signaturePad.minWidth = 6;
        signaturePad.maxWidth = 6;
    });

    document.getElementById('cyan').addEventListener('click', function () {
        signaturePad.penColor = "rgba(0,255,255,0.8)";
        signaturePad.minWidth = 7;
        signaturePad.maxWidth = 7;
    });


    //return signaturePad.toDataURL();
}
function getImageData(element) {
    var result = base64ToByteArray(element.toDataURL("image/png"));
    return result;
}
function base64ToByteArray(base64String) {
    // Remove data URL prefix (if any)
    const base64Image = base64String.replace(/^data:image\/(png|jpg|jpeg);base64,/, '');

    // Decode Base64 string to binary data
    const binaryString = atob(base64Image);

    // Create a byte array to store the binary data
    const byteArray = new Uint8Array(binaryString.length);

    // Fill the byte array with binary data
    for (let i = 0; i < binaryString.length; i++) {
        byteArray[i] = binaryString.charCodeAt(i);
    }

    return byteArray;
}
function closeModal(modalId) {
    $(modalId).modal('hide');
}
function copyToClipboard(elementId) {
    var element = document.getElementById(elementId);

    // Extract text content without including the button's icon
    var textToCopy = Array.from(element.childNodes)
        .filter(node => node.nodeType === Node.TEXT_NODE)
        .map(node => node.textContent.trim())
        .join('');

    navigator.clipboard.writeText(textToCopy)
        .then(() => {
            alert('Copied to clipboard!');
        })
        .catch(err => {
            console.error('Failed to copy text: ', err);
        });
}


