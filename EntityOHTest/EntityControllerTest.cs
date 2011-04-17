using EntityOH.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EntityOHTest
{
    
    
    /// <summary>
    ///This is a test class for EntityControllerTest and is intended
    ///to contain all EntityControllerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class EntityControllerTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            EntityOH.Controllers.Connections.SmartConnection.DefaultConnectionKey="SqlDb";
        }
        
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        

        [TestMethod()]
        public void SelectTest()
        {
            using (var pc = new EntityController<Entities.Person>())
            {

                var all = pc.Select();


                foreach (var r in all) r.Name = "";
                pc.Insert(all);

                
            }
            
        }

        [TestMethod]
        public void CreateInheritedEntity()
        {
            using (var ec = new EntityController<Entities.Employee>())
            {
                var el = ec.Select().ToArray();

            }
        }
    }
}
