using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BatchWrapperTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestParameter1Sweep()
        {
            string[] args =
            {
                "test1.txt",
                "Hemodynamics.Burkhoff_Kofranek.13.10.fmu",
                "modelInputsBlock1.TotalBloodVolume.k", "5000",
                "modelInputsBlock1.TotalUnstressedVolume.k", "2000", "4500", "10",
                "time/leftHeartBurkhofWithBusConnector.LeftAtrium.Pressure",
                "0", "120", "2400", "100"
            };
            File.Delete(args[0]);
            //test that calling returns 0, returning -1 is error
            Assert.AreEqual(0,FmiBatchWrapper.Program.Main(args));
            //test that file exists
            Assert.IsTrue(File.Exists(args[0]));

        }

        [TestMethod]
        public void TestParameters2Sweep()
        {
            string[] args =
            {
                "test2.txt",
                "Hemodynamics.Burkhoff_Kofranek.13.10.fmu",
                "modelInputsBlock1.TotalBloodVolume.k", "5000",
                "modelInputsBlock1.TotalUnstressedVolume.k/modelInputsBlock1.RaSys.k", "2000/0.55", "4500/0.95", "5/2",
                "leftHeartBurkhofWithBusConnector.LeftAtrium.Volume/leftHeartBurkhofWithBusConnector.LeftAtrium.Pressure/leftHeartBurkhofWithBusConnector.LeftVentricle.Volume/leftHeartBurkhofWithBusConnector.LeftVentricle.Pressure",
                "0", "120", "2400", "100"
            };
            File.Delete(args[0]);
            //test that calling returns 0, returning -1 is error
            Assert.AreEqual(0, FmiBatchWrapper.Program.Main(args));
            //test that file exists
            Assert.IsTrue(File.Exists(args[0]));

        }

        [TestMethod]
        public void TestGenerateOnedimSweep()
        {
            var steps = new int[] {100};
            var starts = new double[] {0};
            var stops = new double[] {100};
            var parameters = FmiBatchWrapper.Program.generatesweep(steps, starts, stops);
            
            foreach (var parameter in parameters)
            {
                var parinstance = parameter.ToArray();
                Console.Write("parameters: ");//,parameter.ToArray());
                foreach (var parvalue in parinstance) Console.Write(parvalue+" ");
                Console.WriteLine("");
            }
            Assert.AreEqual(101,parameters.Count());
        }
        [TestMethod]
        public void TestGenerate2DSweep()
        {
            var steps = new int[] { 100,20 };
            var starts = new double[] { 0,100 };
            var stops = new double[] { 100,200 };
            var parameters = FmiBatchWrapper.Program.generatesweep(steps, starts, stops);

            foreach (var parameter in parameters)
            {
                var parinstance = parameter.ToArray();
                Console.Write("parameters: ");//,parameter.ToArray());
                foreach (var parvalue in parinstance) Console.Write(parvalue+" ");
                Console.WriteLine("");
            }
            Assert.AreEqual(101*21, parameters.Count());
        }
    }
}
