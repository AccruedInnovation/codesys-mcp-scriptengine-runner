using _3S.CoDeSys.Core.ComponentModel;
using _3S.CoDeSys.ScriptEngine;
using CODESYS.DevelopmentSystemMCPServer.Registration;
using CODESYS.DevelopmentSystemMCPServer.Results;

namespace AccruedInnovation.CodesysMcp.ScriptRunner
{
    internal sealed class McpDependencyBag : IDependencyInjectable
    {
        public McpDependencyBag()
        {
            _3S.CoDeSys.Core.ComponentModel.ComponentModel.Singleton
                .InjectDependencies(this, GetType());
        }

        [InjectSingleInstance(Shared = true)]
        public ISharedSingleInstanceProvider<IMcpRegistry> McpRegistryProvider
        {
            get;
            set;
        } = null!;

        [InjectSingleInstance(Shared = true)]
        public ISharedSingleInstanceProvider<IMcpNamespaceFactory>
            NamespaceFactoryProvider
        {
            get;
            set;
        } = null!;

        [InjectSingleInstance(Shared = true)]
        public ISharedSingleInstanceProvider<IToolResultFactory>
            ToolResultFactoryProvider
        {
            get;
            set;
        } = null!;

        [InjectSingleInstance(Shared = true)]
        public ISharedSingleInstanceProvider<IScriptEngine2> ScriptEngineProvider
        {
            get;
            set;
        } = null!;

        public void InjectionComplete()
        {
        }
    }
}
