using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using FMUSingleNodeWrapper;
using FMUSingleNodeWrapper.service;

namespace FmiBatchWrapper
{
    class Program
    {
        private static string[] _parameterSweepNames;
        private static double[] _parameterSweepStartValues;
        private static double[] _parameterSweepStopValues;
        private static double[] _parameterSweepSteps;
        private static SimulateService _simulator;
        private static Simulate _request;
        private static string[] _parameterNames;
        private static double[] _parameterValues;
        private static int _lastStepsToRemember;
        private static string _resultfilename;

        private static int Main(string[] args)
        {
            _simulator = new SimulateService();
            _request = new Simulate();
            if (args.Length < 12) { help();
                return -1;
            }
            _resultfilename = args[0]; 
            _request.ModelName = args[1];
            _parameterNames = SplitSlash(args[2]);
            _parameterValues = DoubleSplitSlash(args[3]);
            _parameterSweepNames = SplitSlash(args[4]);
            _parameterSweepStartValues = DoubleSplitSlash(args[5]);
            _parameterSweepStopValues = DoubleSplitSlash(args[6]);
            _parameterSweepSteps = DoubleSplitSlash(args[7]);
            _request.VariableNames = SplitSlash(args[8]);
            _request.Start = int.Parse(args[9]);
            _request.Stop = int.Parse(args[10]);
            _request.Steps = int.Parse(args[11]);
            _lastStepsToRemember = int.Parse(args[12]);
            try
            {
                GenerateParameterInstance(0, new List<double>());
            }
            catch (Exception e)
            {
                Console.WriteLine("error during computation or processing: \n{0}",e.StackTrace);
                return -1;
            }
            //var result = simulator.Any(request);
            return 0;
        }

        private static void help()
        {
            Console.WriteLine("error, bad number of arguments\n" +
                              "FMIBatchWrapper [resultsname] [modelname] [paramnames] [paramvalues] [paramsweppnames]" +
                              " [parametersweepstartvalues] [parametersweepstopvalues] [parametersweepsteps] [variabletoreturn]"+
                              " [starttime] [stoptime] [steps] [stepstoreturn(<=steps)]");
        }

        private static void GenerateParameterInstance(int i,List<double> paramvalues)
        {
            //var paramName = _parameterSweepNames[i];
            var step = (_parameterSweepStopValues[i] - _parameterSweepStartValues[i])/_parameterSweepSteps[i];
            paramvalues.Add(_parameterSweepStartValues[i]);
            for (var paramValue = _parameterSweepStartValues[i];
                paramValue < _parameterSweepStopValues[i];
                paramValue += step)
            {

                paramvalues[i] = paramValue;
                if (i >= _parameterSweepNames.Length)
                {
                    updatesweeprequest(paramvalues);
                    var internalresult = _simulator.Any(_request);
                    var result = new ResultRecord()
                    {
                        ModelName = _request.ModelName,
                        VariableNames = _request.VariableNames,
                        ParameterNames = _request.ParameterNames,
                        ParameterValues = _request.ParameterValues,
                        VariableValues = ((SimulateResponse) internalresult).Result.Reverse().Take(_lastStepsToRemember).Reverse().ToArray()
                    };
                    StoreToFile(_resultfilename,result);
                }
            }
        }

        private static void updatesweeprequest(List<double> paramvalues)
        {
            _request.ParameterNames = _parameterNames.Concat(_parameterSweepNames).ToArray();
            _request.ParameterValues = _parameterValues.Concat(paramvalues).ToArray();
        }

        private static string[] SplitSlash(string arg)
        {
            var separators = new[] {'/'};
            return arg.Split(separators);
        }

        private static double[] DoubleSplitSlash(string arg)
        {
            var numbers = SplitSlash(arg);
            var doublenumber = new List<double>();
            
            ;
            foreach (var number in numbers)
            {
                doublenumber.Add(Double.Parse(number));
            }
            return doublenumber.ToArray();
        }

        public static void StoreToFile(string filename, object resulttostore)
        {
            StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8);
            sw.Write(JsonConvert.SerializeObject(resulttostore));
            sw.Close();
        }


    }

    class ResultRecord
    {
        public string ModelName;
        public string[] ParameterNames;
        public double[] ParameterValues;
        public string[] VariableNames;
        public double[][] VariableValues;

    }
}
