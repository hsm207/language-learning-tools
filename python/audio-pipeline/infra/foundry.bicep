@description('The name of the Azure AI Speech or Cognitive Services account.')
param accountName string

@description('The list of models to deploy. Defaults to an default list! 🤫✨')
param deployments array = []

resource cognitiveAccount 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: accountName
}

@batchSize(1) // Sequential deployment to prevent parent resource conflicts! 🛡️⚖️🏛️
resource modelDeployments 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = [for deployment in deployments: {
  parent: cognitiveAccount // Clean parent-child relationship! 💍💎
  name: deployment.name
  sku: deployment.sku
  properties: {
    model: deployment.model
  }
}]