using System;
using System.Collections.Generic;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Services;
using Moq;
using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace EyeAuras.Tests.UI.Core.Services
{
    [TestFixture]
    public class WindowMatcherFixture
    {
        [Test]
        [TestCaseSource(nameof(ShouldDoMatchingCases))]
        public void ShouldDoMatching(IWindowHandle windowHandle, WindowMatchParams matchParams, bool expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.IsMatch(windowHandle, matchParams);

            //Then
            result.ShouldBe(expected, () => $"Handle: {new { windowHandle.Title, windowHandle.Class, windowHandle.ProcessPath, windowHandle.ProcessName }}, MatchParams: {matchParams}");
        }

        public static IEnumerable<TestCaseData> ShouldDoMatchingCases()
        {
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Handle == new IntPtr(10)), new WindowMatchParams() { Handle = new IntPtr(10)}, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Handle == new IntPtr(10)), new WindowMatchParams() { Title = $"0xA"}, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Handle == new IntPtr(10)), new WindowMatchParams() { Title = $"&HA"}, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessPath == @"C:\app.exe"), new WindowMatchParams() { Title = @"C:\App.exe" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessPath == @"C:\app.exe"), new WindowMatchParams() { Title = @"'C:\aPp.exe'" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessPath == @"C:\app.exe"), new WindowMatchParams() { Title = @"""C:\apP.exe""" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessPath == @"C:\dir1\app.exe"), new WindowMatchParams() { Title = @"dir1\app.exe" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessPath == @"C:\dir2\app.exe"), new WindowMatchParams() { Title = @"dir1\app.exe" }, false);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessPath == @"C:\dir1\app.exe"), new WindowMatchParams() { Title = @"dir.*\\app.exe", IsRegex = true}, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessPath == @"C:\dir2\app.exe"), new WindowMatchParams() { Title = @"dir.*\\app.exe", IsRegex = true}, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessName == "app.exe"), new WindowMatchParams() { Title = @"App.exe" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessName == "app.exe"), new WindowMatchParams() { Title = @"'aPp.exe'" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.ProcessName == "app.exe"), new WindowMatchParams() { Title = @"""apP.exe""" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "logs"), new WindowMatchParams() { Title = @"Logs" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "logs"), new WindowMatchParams() { Title = @"'Logs" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "logs"), new WindowMatchParams() { Title = @"Logs'" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "logs"), new WindowMatchParams() { Title = @"'lOgs'" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "logs"), new WindowMatchParams() { Title = @"""loGs""" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "explorer/logs"), new WindowMatchParams() { Title = @"Logs" }, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "explorer/logs"), new WindowMatchParams() { Title = @"'lOgs'" }, false);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "explorer/logs"), new WindowMatchParams() { Title = @"""loGs""" }, false);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "explorer/app1/logs"), new WindowMatchParams() { Title = @"exp.*Logs", IsRegex = true}, true);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "explorer/app1/log"), new WindowMatchParams() { Title = @"exp.*Logs", IsRegex = true}, false);
            yield return new TestCaseData(Mock.Of<IWindowHandle>(x => x.Title == "explorer/app2/logs"), new WindowMatchParams() { Title = @"/exp.*Logs/" }, true);
        }
        
        private WindowMatcher CreateInstance()
        {
            return new WindowMatcher();
        } 
    }
}