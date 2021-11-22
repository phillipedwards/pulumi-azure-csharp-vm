using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Network;
using Pulumi.AzureNative.Compute;
using System.Collections.Generic;

class MyStack : Stack
{
    public MyStack()
    {
        var config = new Config();
        var username = config.Require("username");

        // Create an Azure Resource Group
        var resourceGroup = new ResourceGroup("resourceGroup", new ResourceGroupArgs
        {
            ResourceGroupName = "vm-extensions-rg"
        });

        var vnet = new VirtualNetwork("vnet-test", new VirtualNetworkArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AddressSpace = new Pulumi.AzureNative.Network.Inputs.AddressSpaceArgs
            {
                AddressPrefixes = { "192.168.0.0/24" }
            },
            Subnets = new List<Pulumi.AzureNative.Network.Inputs.SubnetArgs>
            {
                new Pulumi.AzureNative.Network.Inputs.SubnetArgs
                {
                    Name = "default",
                    AddressPrefix = "192.168.0.0/28"
                }
            }
        });

        var publicIp = new PublicIPAddress("public-ip", new PublicIPAddressArgs
        {
            ResourceGroupName = resourceGroup.Name,
            PublicIPAllocationMethod = "Dynamic"
        });

        var nic = new NetworkInterface("nic", new NetworkInterfaceArgs
        {
            ResourceGroupName = resourceGroup.Name,
            IpConfigurations = new List<Pulumi.AzureNative.Network.Inputs.NetworkInterfaceIPConfigurationArgs>
            {
                new Pulumi.AzureNative.Network.Inputs.NetworkInterfaceIPConfigurationArgs
                {
                    Name = "test-nic-ip",
                    Subnet = new Pulumi.AzureNative.Network.Inputs.SubnetArgs
                    {
                        Id = vnet.Subnets.First().Apply(t => t.Id)!,
                    },
                    PrivateIPAllocationMethod = "Dynamic",
                    PublicIPAddress = new Pulumi.AzureNative.Network.Inputs.PublicIPAddressArgs
                    {
                        Id = publicIp.Id
                    }
                }
            }
        });

        var password = new Pulumi.Random.RandomPassword("vm-password", new Pulumi.Random.RandomPasswordArgs
        {
            Length = 16
        });

        var random = new System.Random();
        var vm = new VirtualMachine("az-vm", new VirtualMachineArgs
        {
            ResourceGroupName = resourceGroup.Name,
            NetworkProfile = new Pulumi.AzureNative.Compute.Inputs.NetworkProfileArgs
            {
                NetworkInterfaces = new List<Pulumi.AzureNative.Compute.Inputs.NetworkInterfaceReferenceArgs>
                {
                    new Pulumi.AzureNative.Compute.Inputs.NetworkInterfaceReferenceArgs { Id = nic.Id }
                }
            },
            HardwareProfile = new Pulumi.AzureNative.Compute.Inputs.HardwareProfileArgs
            {
                VmSize = VirtualMachineSizeTypes.Standard_A0
            },
            OsProfile = new Pulumi.AzureNative.Compute.Inputs.OSProfileArgs
            {
                ComputerName = "test-host",
                AdminUsername = username,
                AdminPassword = password.Result,
            },
            StorageProfile = new Pulumi.AzureNative.Compute.Inputs.StorageProfileArgs
            {
                OsDisk = new Pulumi.AzureNative.Compute.Inputs.OSDiskArgs
                {
                    CreateOption = DiskCreateOptionTypes.FromImage,
                    Name = "vm-ext-disk-q34"
                },
                ImageReference = new Pulumi.AzureNative.Compute.Inputs.ImageReferenceArgs
                {
                    Publisher = "MicrosoftWindowsServer",
                    Offer = "WindowsServer",
                    Sku = "2019-Datacenter",
                    Version = "latest"
                }
            }
        });

        // ProtectedSettings and Settings can be used to populate the CustomScriptExtension VM Extension for Windows machines
        // ProtectedSetting is automatically encrypted on the target host, while Settings is protected.
        // 'commandToExecute' and 'FileUris' are the only two properties allowed in ProtectedSettings and each field can only be present in ProtectedSettings or Settings dictionary; not both.
        // https://docs.microsoft.com/en-us/azure/virtual-machines/extensions/custom-script-windows
        // The dictionaries themselves are just <string, string> dictionaries, so treat them like any C# string, although the use of 'Apply' and/or 'Tuple' may be necessary if you want to use outputs of resources, in your commands, as below.        
        var fileName = "textFile.txt";
        var itemType = "file";

        var vmExtension = vm.Name.Apply(name => 
        {
            return new VirtualMachineExtension("az-vm-extension", new VirtualMachineExtensionArgs
            {
                ResourceGroupName = resourceGroup.Name,
                VmName = vm.Name,
                Publisher = "Microsoft.Compute",
                Type = "CustomScriptExtension",
                TypeHandlerVersion = "1.10",
                AutoUpgradeMinorVersion = true,
                ProtectedSettings = new Dictionary<string, string>
                {
                    { "commandToExecute", $"powershell.exe New-Item -Path . -Name '{fileName}' -ItemType '{itemType}' -Value 'the created VM name is {name}'" }
                },
                Settings = new Dictionary<string, string>
                {
                    { "timestamp", System.DateTime.Parse("2021-11-22").ToString() }
                }
            });
        });

        VmPassword = password.Result;
    }

    [Output]
    public Output<string> VmPassword { get; set; }
}