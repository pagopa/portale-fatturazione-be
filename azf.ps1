$uri = "https://fat-u-integration-func-staging.scm.azurewebsites.net/api/registry/webhook"
$authUser = "fat-u-integration-func__staging"
$authPassword = $env:AZF_WEBHOOK_PASSWORD
$headers = @{ "Content-Type" = "application/json" }

if (-not $authPassword) {
	throw "Set the AZF_WEBHOOK_PASSWORD environment variable before running this script."
}

$securePassword = ConvertTo-SecureString $authPassword -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($authUser, $securePassword)

$response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body '{}' -Credential $credential

$response

