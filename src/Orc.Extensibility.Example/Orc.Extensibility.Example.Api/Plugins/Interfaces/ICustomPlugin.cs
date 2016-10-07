// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICustomPlugin.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example
{
    using System.Threading.Tasks;

    public interface ICustomPlugin
    {
        Task InitializeAsync();
    }
}