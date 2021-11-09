using System.Threading.Tasks;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Network;
using Pulumi.AzureNative.Compute;

using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json.Linq;

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
                LinuxConfiguration = new Pulumi.AzureNative.Compute.Inputs.LinuxConfigurationArgs { DisablePasswordAuthentication = false }
            },
            StorageProfile = new Pulumi.AzureNative.Compute.Inputs.StorageProfileArgs
            {
                OsDisk = new Pulumi.AzureNative.Compute.Inputs.OSDiskArgs
                {
                    CreateOption = DiskCreateOptionTypes.FromImage,
                    Name = "vm-ext-disk"
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

        var settings = new JObject();
        settings["fileUris"] = "";

        var protectedSettings = new JObject();
        protectedSettings["commandToExecute"] = "powershell.exe -File \"./scripts/\"";

        var vmExtension = new VirtualMachineExtension("az-vm-extension", new VirtualMachineExtensionArgs
        {
            ResourceGroupName = resourceGroup.Name,
            VmName = vm.Name,
            Publisher = "Microsoft.Compute",
            Type = "CustomScriptExtension",
            TypeHandlerVersion = "1.10",
            AutoUpgradeMinorVersion = true,
            Settings = settings.ToString(),
            ProtectedSettings = protectedSettings.ToString()
        });

        IpAddress = publicIp.IpAddress!;
        VmPassword = password.Result;
    }

    [Output]
    public Output<string> IpAddress { get; set;}

    [Output]
    public Output<string> VmPassword { get; set; }
}
