const resendEmailElement = $("#trigger-resend-email");
const resendEmailElementInitialText = resendEmailElement.text();

resendEmailElement.click(() => {
    const requestVerificationToken = $("input[name='__RequestVerificationToken']").val();
    const userEmail = $("input[name='Email']").val();
    const resendEmailRecordId = $("input[name='ResendEmailRecordId']").val();

    const requestBody = {
        Email: userEmail,
        ResendEmailRecordId: resendEmailRecordId,
        __RequestVerificationToken: requestVerificationToken
    };

    $.ajax({
        url: "/clientservices/correspondence/resendemail",
        method: "POST",
        data: requestBody,
        cache: false,
        success: () => {
            resendEmailElement.text("Email resent")
        },
        error: (xhr) => {
            const responseText = xhr.responseText || "Could not resend. Please try again.";

            resendEmailElement.text(responseText);
        },
        complete: () => {
            setTimeout(() => resendEmailElement.text(resendEmailElementInitialText), 5000);
        }
    })
})