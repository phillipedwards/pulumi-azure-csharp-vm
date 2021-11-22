# Pulumi Azure Virtual Machine w/ Virutal Machine Extension
This repo shows how to create an Azure VM Extension of type CustomScriptExtension to an Azure VM. This example will use a contrived `powershell` script example, although more complex use cases is supported.  

Read more about Azure Virutal Machine Extensions [here](https://docs.microsoft.com/en-us/azure/virtual-machines/extensions/custom-script-windows).

## Steps to use repo
1. Clone this repo. (git clone ...)
2. Run `pulumi stack init` and fill out required details.
3. Set your Azure location using `pulumi config set azure-native:location {location}`
4. Run `pulumi up` and view the resources created.
5. Remember to destroy any resources you do not need!
