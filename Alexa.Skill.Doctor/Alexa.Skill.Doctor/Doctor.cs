using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Newtonsoft.Json;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Alexa.Skill.Doctor
{
    public class Doctor
    {
        private ILambdaLogger loggerGlobal;

        public List<FactResource> GetResources()
        {
            List<FactResource> resources = new List<FactResource>();
            FactResource enINResource = new FactResource("en-IN");
            enINResource.SkillName = "Family Doctor";
            enINResource.HelpMessage = "You can say, ask Family Doctor, suggest medicine for Headache for an adult, or suggest medicine for fever for a kid";
            enINResource.HelpReprompt = String.Empty;
            enINResource.StopMessage = String.Empty;
            enINResource.Facts.Add("Please speak your desired operation clearly...");
            resources.Add(enINResource);
            return resources;
        }

        public string emitNewFact(FactResource resource, bool withPreface)
        {
            Random r = new Random();
            if (withPreface)
                return resource.GetFactMessage + resource.Facts[r.Next(resource.Facts.Count)];
            return resource.Facts[r.Next(resource.Facts.Count)];
        }

        /// <summary>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse DoctorHandler(SkillRequest input, ILambdaContext context)
        {
            SkillResponse response = new SkillResponse();
            response.Response = new ResponseBody();
            response.Response.ShouldEndSession = false;
            IOutputSpeech innerResponse = null;
            var log = context.Logger;
            loggerGlobal = log;
            log.LogLine($"Skill Request Object:");
            log.LogLine(JsonConvert.SerializeObject(input));

            var allResources = GetResources();
            var resource = allResources.FirstOrDefault();

            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"Default LaunchRequest made: 'Alexa, ask Family Doctor");
                innerResponse = new PlainTextOutputSpeech();
                (innerResponse as PlainTextOutputSpeech).Text = "You can say, ask Family Doctor, suggest medicine for Headache for an adult, or suggest medicine for fever for a kid";

            }
            else if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = (IntentRequest)input.Request;
                string SYMPTOM = null;
                string AGE = intentRequest.Intent.Slots["AGE"].Value;
                List<ResolutionAuthority> auths = intentRequest.Intent.Slots["SYMPTOM"].Resolution.Authorities.ToList();
                auths.ForEach(a => {
                    SYMPTOM = a.Values[0].Value.Name;
                });

                log.LogLine($"-------------------------------------------------------------");
                log.LogLine($"INTENT RESOLVER received Intent - " + intentRequest.Intent.Name);
                log.LogLine($"SLOT RESOLVER received Slots - " + SYMPTOM + ", " + AGE);
                log.LogLine($"-------------------------------------------------------------");

                try
                {
                    string responseText = String.Empty;
                    switch (intentRequest.Intent.Name)
                    {
                        case "AMAZON.CancelIntent":
                            log.LogLine($"AMAZON.CancelIntent: send StopMessage");
                            innerResponse = new PlainTextOutputSpeech();
                            (innerResponse as PlainTextOutputSpeech).Text = resource.StopMessage;
                            response.Response.ShouldEndSession = true;
                            break;
                        case "AMAZON.StopIntent":
                            log.LogLine($"AMAZON.StopIntent: send StopMessage");
                            innerResponse = new PlainTextOutputSpeech();
                            (innerResponse as PlainTextOutputSpeech).Text = resource.StopMessage;
                            response.Response.ShouldEndSession = true;
                            break;
                        case "AMAZON.HelpIntent":
                            log.LogLine($"AMAZON.HelpIntent: send HelpMessage");
                            innerResponse = new PlainTextOutputSpeech();
                            (innerResponse as PlainTextOutputSpeech).Text = resource.HelpMessage;
                            break;
                        case "SuggestMedicine":
                            log.LogLine($"SuggestMedicine sent: slot values:" + SYMPTOM + ", " + AGE);
                            innerResponse = new PlainTextOutputSpeech();
                            responseText = GetAdvice(SYMPTOM, AGE);
                            (innerResponse as PlainTextOutputSpeech).Text = responseText;
                            response.Response.ShouldEndSession = true;
                            break;
                        default:
                            log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                            innerResponse = new PlainTextOutputSpeech();
                            (innerResponse as PlainTextOutputSpeech).Text = resource.HelpMessage;
                            response.Response.ShouldEndSession = true;
                            break;
                    }
                }
                catch (Exception exp)
                {
                    log.LogLine($"EXCEPTION: " + exp.Message + Environment.NewLine + exp.StackTrace);
                    innerResponse = new PlainTextOutputSpeech();
                    (innerResponse as PlainTextOutputSpeech).Text = "Sorry, I could not clearly understand the last request. Could you please repeat that...";
                    response.Response.ShouldEndSession = true;
                }
            }

            response.Response.OutputSpeech = innerResponse;
            response.Version = "1.0";
            log.LogLine($"Skill Response Object...");
            log.LogLine(JsonConvert.SerializeObject(response));
            return response;
        }


        private string GetAdvice(string _Symptoms, string Age)
        {
            string retVal = String.Empty;
            Regimen r = SuggestMedicine(_Symptoms, Age);
            if (r != null)
            {
                retVal += String.Format("Please use {0}. ", r.MedicineNames[0]);
                if (String.IsNullOrEmpty(Age))
                {
                    retVal += String.Format("{0} for adult and {1} for a kid.", r.DosageAdult, r.DosageKid);
                }
                else
                {
                    switch (Age.ToUpper())
                    {
                        case "ADULT":
                            retVal += String.Format("{0} for adults. ", r.DosageAdult);
                            break;
                        case "KID":
                        case "CHILD":
                            retVal += String.Format("{0} for kids. ", r.DosageKid);
                            break;
                        case "INFANT":
                        case "BABY":
                        case "TODDLER":
                            retVal += " For infants please consult your doctor about safe dosage.";
                            break;
                        default:
                            break;
                    }
                }
                retVal += String.Format(" Alternate medicines are {0}.", r.AltMedicineNames[0]);
                retVal += String.Format(" Generic composition is {0}", r.MedicineComposition[0]);
            }
            else
            {
                retVal = "Sorry, I could not find a suggested treatment for your symptoms. Please consult a doctor.";
            }
            return retVal;
        }


        private Regimen SuggestMedicine(string _Symptoms, string Age)
        {
            bool treatmentFound = false;
            Regimen retVal = null;
            TreatmentRegimens model = JsonConvert.DeserializeObject<TreatmentRegimens>(File.ReadAllText(@"./data.json").ToUpper());
            model.RegimenData.ForEach(r => {
                if ((!treatmentFound) && (r.Symptoms.Contains(_Symptoms.ToUpper())))
                {
                    treatmentFound = true;
                    retVal = r;
                }
            });

            return retVal;
        }
    }
}
