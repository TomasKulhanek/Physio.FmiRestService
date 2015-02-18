using ServiceStack.ServiceInterface;

namespace FMUSingleNodeWrapper.service
{
    public class SimulateListService : Service
    {
       
        public object Get(SimulateList request)
        {
            return new SimulateListResponse { ModelNames = NodeServerParams.FMUFiles.ToArray() };
        }
         
    }
    //DTO response
    public class SimulateListResponse
    {
        public string [] ModelNames { get; set; }
    }

    //DTO request

    public class SimulateList
    {
        public string ModelName { get; set; }
    }

}