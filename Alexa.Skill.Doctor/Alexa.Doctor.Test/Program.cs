using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alexa.Skill.Doctor;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Alexa.Doctor.Test
{
    class Program
    {
        static TreatmentRegimens model;
        static void Main(string[] args)
        {
            //model = new TreatmentRegimens();
            //CreateRegimens();
            //WriteJson();
            model = null;
            model = JsonConvert.DeserializeObject<TreatmentRegimens>(File.ReadAllText(@"..\data.json").ToUpper());
            Regimen r = Suggest("Cold","adult");
        }

        static void CreateRegimens()
        {
            for (int i = 0; i < 5; i++)
            {
                Regimen r = new Regimen();
                r.Symptoms.Add("headache");
                r.MedicineComposition.Add("Paracetamol");
                r.MedicineNames.Add("Crocin");
                r.AltMedicineNames.Add("Crocin DS");
                r.DosageAdult = "1 tablet 8 hourly";
                r.DosageKid = "10 ml 8 hourly";
                model.RegimenData.Add(r);
            }
        }

        static void WriteJson()
        {
            File.WriteAllText(@"..\data.json", JsonConvert.SerializeObject(model).ToUpper());
        }

        static Regimen Suggest(string _Symptoms, string _Age)
        {
            bool treatmentFound = false;
            Regimen retVal = null;

            model.RegimenData.ForEach(r => {
                if ((!treatmentFound)&&(r.Symptoms.Contains(_Symptoms.ToUpper())))
                {
                    treatmentFound = true;
                    retVal = r;
                }
            });

            return retVal;
        }
    }
}
