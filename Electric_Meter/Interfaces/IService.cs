using Electric_Meter.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Interfaces
{
    public interface IService
    {
        Task<int> InsertToMachine(Machine machine);
        Task<List<DvElectricDataTemp>> GetListDataAsync(int address);
        Task<int> EditToMachine(Machine machine);
        Task<int> DeleteToMachine(Machine machine);
        Task InsertToElectricDataTempAsync(DvElectricDataTemp dvElectricDataTemp);
    }
}
