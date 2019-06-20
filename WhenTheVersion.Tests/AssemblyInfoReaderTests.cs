using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhenTheVersion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WhenTheVersion.Tests
{
    [TestClass()]
    public class AssemblyInfoReaderTests
    {
        string _projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;

        [TestMethod()]
        public void GetRevisionInfoTestCaseStraightForward()
        {
            AssemblyInfoReader assemblyInfoReader = new AssemblyInfoReader(Path.Combine(_projectDirectory, "AssemblyInfoTestCase1.cs"));
            var revisionInfo = assemblyInfoReader.GetRevisionInfo();
            //In this case it should not fail
            Assert.AreEqual(true, revisionInfo.Succeed);
        }


        [TestMethod()]
        public void GetRevisionInfoTestCaseWithAsterisks()
        {
            AssemblyInfoReader assemblyInfoReader = new AssemblyInfoReader(Path.Combine(_projectDirectory, "AssemblyInfoTestCase2.cs"));
            var revisionInfo = assemblyInfoReader.GetRevisionInfo();
            //In this case it should not succeed
            Assert.AreEqual(true, !revisionInfo.Succeed);
            
        }

        [TestMethod()]
        public void GetRevisionInfoTestCaseWithComments()
        {
            AssemblyInfoReader assemblyInfoReader = new AssemblyInfoReader(Path.Combine(_projectDirectory, "AssemblyInfoTestCase3.cs"));
            var revisionInfo = assemblyInfoReader.GetRevisionInfo();
            //In this case it should succeed
            Assert.AreEqual(true, revisionInfo.Succeed);
            Assert.AreEqual(16, revisionInfo.NextRevisionNumber);
        }


        [TestMethod()]
        public void GetRevisionInfoTestCaseMissingAssemblyVersionInfo()
        {
            AssemblyInfoReader assemblyInfoReader = new AssemblyInfoReader(Path.Combine(_projectDirectory, "AssemblyInfoTestCase4.cs"));
            var revisionInfo = assemblyInfoReader.GetRevisionInfo();
            //This needs to fail as file doesn't have AssemblyInfo line
            Assert.AreEqual(false, revisionInfo.Succeed);
        }

        [TestMethod()]
        [ExpectedException(typeof(FileNotFoundException))]
        public void GetRevisionInfoTestCaseMissingFile()
        {
            AssemblyInfoReader assemblyInfoReader = new AssemblyInfoReader(Path.Combine(_projectDirectory, "AssemblyInfoTestCase6.cs"));
            //This needs to throw FileNotFoundException
            var revisionInfo = assemblyInfoReader.GetRevisionInfo();
        }
    }
}