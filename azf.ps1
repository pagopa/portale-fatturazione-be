$uri = "https://fat-u-integration-func-staging.scm.azurewebsites.net/api/registry/webhook"
$auth = "fat-u-integration-func__staging:l8DctrfAt0g48crW0QAGwucdXMtAuXdhd90c7HTgssh8xcucSoyg0AGqRH9Y"
$headers = @{ "Content-Type" = "application/json" }

$response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body '{}' -Credential (New-Object System.Management.Automation.PSCredential($auth.Split(":")[0], (ConvertTo-SecureString $auth.Split(":")[1] -AsPlainText -Force)))

$response
