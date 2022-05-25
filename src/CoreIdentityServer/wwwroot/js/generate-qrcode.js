const totpAuthQRCodeContainer = document.getElementById("totp-auth-qr-code");
const authenticatorKey = $("input[name='AuthenticatorKey']").val();
const authenticatorKeyUri = $("input[name='AuthenticatorKeyUri']").val();

QRCode
    .toCanvas(
        totpAuthQRCodeContainer,
        authenticatorKeyUri,
        { scale: 2 },
        (error) => { if (error) { console.error(error) }}
    );

const copySecretKeyText = $("#copy-secret-key");
const copySecretKeyTextInitialValue = copySecretKeyText.text();

copySecretKeyText.click(() => {
    navigator.clipboard.writeText(authenticatorKey)
        .then(() => copySecretKeyText.text("Copied to clipboard"))
        .catch(() => copySecretKeyText.text("Couldn't copy! Try again"))

    setTimeout(() => copySecretKeyText.text(copySecretKeyTextInitialValue), 5000);
})
