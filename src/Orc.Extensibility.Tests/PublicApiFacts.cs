// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PublicApiFacts.cs" company="WildGums">
//   Copyright (c) 2008 - 2017 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Tests
{
    using System.Runtime.CompilerServices;
    using ApiApprover;
    using NUnit.Framework;

    [TestFixture]
    public class PublicApiFacts
    {
        [Test, MethodImpl(MethodImplOptions.NoInlining)]
        public void Orc_Extensibility_HasNoBreakingChanges()
        {
            var assembly = typeof(PluginFactory).Assembly;

            PublicApiApprover.ApprovePublicApi(assembly);
        }
    }
}