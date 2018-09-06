using System;
using System.Collections.Generic;
using System.Text;

namespace Alexa.Skill.Doctor
{
    public class Regimen
    {
        public List<string> Symptoms { get; set; }
        public List<string> MedicineComposition { get; set; }
        public List<string> MedicineNames { get; set; }
        public string DosageAdult { get; set; }
        public string DosageKid { get; set; }
        public List<string> AltMedicineNames { get; set; }

        public Regimen()
        {
            Symptoms = new List<string>();
            MedicineComposition = new List<string>();
            MedicineNames = new List<string>();
            AltMedicineNames = new List<string>();
        }
    }
}
