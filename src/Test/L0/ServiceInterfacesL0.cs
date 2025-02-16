// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Capabilities;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using Microsoft.VisualStudio.Services.Agent.Worker.Release;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Microsoft.VisualStudio.Services.Agent.Worker.LegacyTestResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine;
using Microsoft.VisualStudio.Services.Agent.Worker.Maintenance;
using Microsoft.VisualStudio.Services.Agent.Listener.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ServiceInterfacesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void AgentInterfacesSpecifyDefaultImplementation()
        {
            // Validate all interfaces in the Listener assembly define a valid service locator attribute.
            // Otherwise, the interface needs to whitelisted.
            var whitelist = new[]
            {
                typeof(ICredentialProvider),
                typeof(IConfigurationProvider),
                typeof(IDiagnostic)
            };
            Validate(
                assembly: typeof(IMessageListener).GetTypeInfo().Assembly,
                whitelist: whitelist);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CommonInterfacesSpecifyDefaultImplementation()
        {
            // Validate all interfaces in the Common assembly define a valid service locator attribute.
            // Otherwise, the interface needs to whitelisted.
            var whitelist = new[]
            {
                typeof(IAgentService),
                typeof(ICredentialProvider),
                typeof(IExtension),
                typeof(IHostContext),
                typeof(ITraceManager),
                typeof(IThrottlingReporter),
                typeof(ICapabilitiesProvider)
            };
            Validate(
                assembly: typeof(IHostContext).GetTypeInfo().Assembly,
                whitelist: whitelist);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void WorkerInterfacesSpecifyDefaultImplementation()
        {
            // Validate all interfaces in the Worker assembly define a valid service locator attribute.
            // Otherwise, the interface needs to whitelisted.
            var whitelist = new[]
            {
                typeof(IArtifactDetails),
                typeof(IArtifactExtension),
                typeof(ICodeCoverageSummaryReader),
                typeof(IExecutionContext),
                typeof(IHandler),
                typeof(IJobExtension),
                typeof(ISourceProvider),
                typeof(IStep),
                typeof(IStepHost),
                typeof(ITfsVCMapping),
                typeof(ITfsVCPendingChange),
                typeof(ITfsVCShelveset),
                typeof(ITfsVCStatus),
                typeof(ITfsVCWorkspace),
                typeof(IWorkerCommandExtension),
                typeof(IContainerProvider),
                typeof(IMaintenanceServiceProvider),
                typeof(IDiagnosticLogManager),
                typeof(IParser),
                typeof(IResultReader),
                typeof(INUnitResultsXmlReader),
                typeof(IWorkerCommand),
                typeof(IWorkerCommandRestrictionPolicy)
            };
            Validate(
                assembly: typeof(IStepsRunner).GetTypeInfo().Assembly,
                whitelist: whitelist);
        }

        private static void Validate(Assembly assembly, params Type[] whitelist)
        {
            // Iterate over all non-whitelisted interfaces contained within the assembly.
            IDictionary<TypeInfo, Type> w = whitelist.ToDictionary(x => x.GetTypeInfo());
            foreach (TypeInfo interfaceTypeInfo in assembly.DefinedTypes.Where(x => x.IsInterface && !w.ContainsKey(x)))
            {
                // Temporary hack due to shared code copied in two places.
                if (interfaceTypeInfo.FullName.StartsWith("Microsoft.TeamFoundation.DistributedTask"))
                {
                    continue;
                }

                if (interfaceTypeInfo.FullName.Contains("IConverter")){
                    continue;
                }

                // Assert the ServiceLocatorAttribute is defined on the interface.
                CustomAttributeData attribute =
                    interfaceTypeInfo
                    .CustomAttributes
                    .SingleOrDefault(x => x.AttributeType == typeof(ServiceLocatorAttribute));
                Assert.True(attribute != null, $"Missing {nameof(ServiceLocatorAttribute)} for interface '{interfaceTypeInfo.FullName}'. Add the attribute to the interface or whitelist the interface in the test.");

                // Assert the interface is mapped to a concrete type.
                // Also check platform-specific interfaces if they exist
                foreach (string argName in new string[] {
                    nameof(ServiceLocatorAttribute.Default),
                    nameof(ServiceLocatorAttribute.PreferredOnWindows),
                    nameof(ServiceLocatorAttribute.PreferredOnMacOS),
                    nameof(ServiceLocatorAttribute.PreferredOnLinux),
                })
                {
                    CustomAttributeNamedArgument arg =
                        attribute
                        .NamedArguments
                        .SingleOrDefault(x => String.Equals(x.MemberName, argName, StringComparison.Ordinal));

                    if (arg.TypedValue.Value is null && !argName.Equals(nameof(ServiceLocatorAttribute.Default)))
                    {
                        // a non-"Default" attribute isn't present, which is OK
                        continue;
                    }

                    Type concreteType = arg.TypedValue.Value as Type;
                    string invalidConcreteTypeMessage = $"Invalid {argName} parameter on {nameof(ServiceLocatorAttribute)} for the interface '{interfaceTypeInfo.FullName}'. The implementation must not be null, must not be an interface, must be a class, and must implement the interface '{interfaceTypeInfo.FullName}'.";
                    Assert.True(concreteType != null, invalidConcreteTypeMessage);
                    TypeInfo concreteTypeInfo = concreteType.GetTypeInfo();
                    Assert.False(concreteTypeInfo.IsInterface, invalidConcreteTypeMessage);
                    Assert.True(concreteTypeInfo.IsClass, invalidConcreteTypeMessage);
                    Assert.True(concreteTypeInfo.ImplementedInterfaces.Any(x => x.GetTypeInfo() == interfaceTypeInfo), invalidConcreteTypeMessage);
                }
            }
        }
    }
}
