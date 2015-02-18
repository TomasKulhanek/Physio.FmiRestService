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
using ServiceStack.Html;

namespace FmiBatchWrapper
{
    public class Program
    {
        private static string[] _parameterSweepNames;
        private static double[] _parameterSweepStartValues;
        private static double[] _parameterSweepStopValues;
        private static int[] _parameterSweepSteps;
        private static SimulateService _simulator;
        private static Simulate _request;
        private static string[] _parameterNames;
        private static double[] _parameterValues;
        private static int _lastStepsToRemember;
        private static string _resultfilename;

        public static int Main(string[] args)
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
            _parameterSweepSteps = IntSplitSlash(args[7]);
            _request.VariableNames = SplitSlash(args[8]);
            _request.Start = int.Parse(args[9]);
            _request.Stop = int.Parse(args[10]);
            _request.Steps = int.Parse(args[11]);
            _lastStepsToRemember = int.Parse(args[12]);
            try
            {
                ParameterSweep();
                //GenerateParameterInstance(0, new List<double>());
            }
            catch (Exception e)
            {
                Console.WriteLine("error during computation or processing: {0}\n{1}",e.Message,e.StackTrace);
                return -1;
            }
            //var result = simulator.Any(request);
            return 0;
        }

        private static void help()
        {
            Console.WriteLine("error, bad number of arguments\n" +
                              "FMIBatchWrapper [resultfilename] [modelname] [paramnames] [paramvalues] [paramsweppnames]" +
                              " [parametersweepstartvalues] [parametersweepstopvalues] [parametersweepsteps] [variabletoreturn]"+
                              " [starttime] [stoptime] [steps] [stepstoreturn(<=steps)]");
        }

        public static void ParameterSweep()
        {
            var sequence = generatesweep(_parameterSweepSteps, _parameterSweepStartValues, _parameterSweepStopValues);
            StoreToFileInit(_resultfilename);
            foreach (var item in sequence)
            {
                updatesweeprequest(item);
                var internalresult = _simulator.Any(_request);
                var result = new ResultRecord()
                {
                    ModelName = _request.ModelName,
                    VariableNames = _request.VariableNames,
                    ParameterNames = _request.ParameterNames,
                    ParameterValues = _request.ParameterValues,
                    VariableValues = ((SimulateResponse)internalresult).Result.Reverse().Take(_lastStepsToRemember).Reverse().ToArray()
                };
                StoreToFile(_resultfilename, result);
            }
            StoreToFileFinalize(_resultfilename);
        }


        public static IEnumerable<IEnumerable<double>> generatesweep(int[] sweepSteps, double[] sweepStarts, double[] sweepStops)
        {
            //check length of all arrays in params to be the same
            if ((sweepSteps.Length!=sweepStarts.Length) || (sweepSteps.Length!=sweepStops.Length) || (sweepStarts.Length!=sweepStops.Length))
            throw new Exception("length of parameter values are different");
            //empty eunemrable
            var  paramsset = new List<IEnumerable<double>>();
            //loop over each parameter
            for (int i=0; i< sweepSteps.Length;i++)
            {
                //generate range of each parameter
                var startValue = sweepStarts[i];
                var stopValue = sweepStops[i];
                var steps = sweepSteps[i]+1;
                IEnumerable<double> paraminstance =
                    Enumerable.Range(0, steps)
                        .Select(j => startValue + (stopValue - startValue)*((double) j/(steps-1)));
                //add this range to the set of params
                //if (paramsset == null) paramsset = paraminstance;
                
                paramsset.Add(paraminstance);
            }
            //cartesianproduct - generate all permutation from each range of params
            IEnumerable<IEnumerable<double>> myseq = CartesianProduct(paramsset);
            return myseq;
        } 

        static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
        (accumulator, sequence) =>
          from accseq in accumulator
          from item in sequence
          select accseq.Concat(new[] { item }));
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

        private static void updatesweeprequest(IEnumerable<double> paramvalues)
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

        private static int[] IntSplitSlash(string arg)
        {
            var numbers = SplitSlash(arg);
            var doublenumber = new List<int>();

            ;
            foreach (var number in numbers)
            {
                doublenumber.Add(Int32.Parse(number));
            }
            return doublenumber.ToArray();
        }

        public static void StoreToFileInit(string filename)
        {
            StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8);
            sw.Write("[");
            sw.Close();
            
        }

        public static void StoreToFile(string filename, object resulttostore)
        {
            StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8);
            sw.Write("\n");
            sw.Write(JsonConvert.SerializeObject(resulttostore));
            sw.Write("\n,");
            sw.Close();
        }

        public static void StoreToFileFinalize(string filename)
        {
            StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8);
            sw.Write("{}]");
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
