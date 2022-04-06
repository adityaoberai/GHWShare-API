using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Company.Function
{
    public class RandomWord
    {
        [FunctionName("RandomWord")]
        public async Task Run([TimerTrigger("0 0 8 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            HttpClient client = new HttpClient();

            string randomWordAPI = "https://random-word-api.herokuapp.com/word?number=1";
            var randomWordAPIResponse = await client.GetAsync(randomWordAPI);
            var randomWordList = await randomWordAPIResponse.Content.ReadAsAsync<dynamic>();
            string randomWord = randomWordList[0];
            
            string oxfordDictionaryAPI = "https://od-api.oxforddictionaries.com:443/api/v2/entries/en-gb/"+randomWord.ToLower();
            string appId = Environment.GetEnvironmentVariable("OxfordAppId");
            string apiKey = Environment.GetEnvironmentVariable("OxfordAppKey");
            client.DefaultRequestHeaders.Add("app_id", appId);
            client.DefaultRequestHeaders.Add("app_key", apiKey);
            var oxfordDictionaryAPIResponse = await client.GetAsync(oxfordDictionaryAPI);
            var wordMeaningJson = await oxfordDictionaryAPIResponse.Content.ReadAsStringAsync();
            var wordMeaning = JsonConvert.DeserializeObject<OxfordDictionaryResponse>(wordMeaningJson);
            
            var twilioAccountSID = Environment.GetEnvironmentVariable("TwilioAccountSID");
            var twilioAuthToken = Environment.GetEnvironmentVariable("TwilioAuthToken");
            var twilioNumber = Environment.GetEnvironmentVariable("TwilioPhoneNumber");
            var myNumber = Environment.GetEnvironmentVariable("MyPhoneNumber");
            
            string messageBody = $"WORD OF THE DAY\nThe word of the day is \"{randomWord}\", which means \"{wordMeaning.Results[0].LexicalEntries[0].Entries[0].Senses[0].Definitions[0]}\"";

            TwilioClient.Init(twilioAccountSID, twilioAuthToken);

            var message = MessageResource.Create(
                body: messageBody,
                from: new Twilio.Types.PhoneNumber(twilioNumber),
                to: new Twilio.Types.PhoneNumber(myNumber)
            );

            log.LogInformation(message.Sid);
        }
    }
}
