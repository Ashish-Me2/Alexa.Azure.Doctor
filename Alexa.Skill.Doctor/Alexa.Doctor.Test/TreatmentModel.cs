using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alexa.Skill.Doctor
{
    public class TreatmentRegimens
    {
        public List<Regimen> RegimenData { get; set; }
        public TreatmentRegimens()
        {
            RegimenData = new List<Regimen>();
        }
    }
}
