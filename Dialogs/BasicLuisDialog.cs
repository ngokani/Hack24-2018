using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GiphyDotNet.Manager;
using GiphyDotNet.Model.Parameters;
using LuisBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace Microsoft.Bot.Sample.LuisBot
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"],
            ConfigurationManager.AppSettings["LuisAPIKey"],
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            if (!ShouldBotReply(context)) return;
            StringBuilder noComprendeSB = new StringBuilder("Sorry, I didn't understand");
            if (!string.IsNullOrWhiteSpace(result?.Query))
            {
                noComprendeSB.Append($" what you meant when you said \"{result.Query}\"");
            }

            await context.PostAsync(noComprendeSB.ToString());
            await context.PostAsync("Please try a different term or phrase.");
        }

        // Go to https://luis.ai and create a new intent, then train/publish your luis app.
        // Finally replace "Gretting" with the name of your newly created intent in the following handler
        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            if (!ShouldBotReply(context)) return;
            await context.PostAsync("Hello! How can I help?");
        }

        [LuisIntent("Cancel")]
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            if (!ShouldBotReply(context)) return;
            await this.ShowLuisResult(context, result);
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            if (!ShouldBotReply(context)) return;
            await this.ShowLuisResult(context, result);
        }

        [LuisIntent("Image/Gif")]
        public async Task ImageIntent(IDialogContext context, LuisResult result)
        {
            if (!ShouldBotReply(context)) return;
            var resultMessage = context.MakeMessage();

            string lowResImage = null;
            string image = null;
            var query = result.Entities.FirstOrDefault()?.Entity;
            if (!string.IsNullOrWhiteSpace(query))
            {
                try
                {
                    var giphy = new Giphy("oZy0HYzaXNCmrq0dNOGyiuZgyaaTc3hL");
                    var searchParameter = new SearchParameter()
                    {
                        Query = query
                    };
                    // Returns gif results
                    var gifResult = await giphy.GifSearch(searchParameter);
                    Random rand = new Random();
                    int resultIdx = rand.Next(gifResult.Data.Length - 1);
                    image = gifResult.Data[resultIdx].Images.Original.Url;
                    lowResImage = gifResult.Data[resultIdx].Images.DownsizedStill?.Url ?? image;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            if (string.IsNullOrWhiteSpace(image))
            {
                resultMessage.Text = "Hmmmm, I couldn't find an image for that. Sorry";
            }
            else
            {
                resultMessage.Attachments.Add(new AnimationCard()
                {
                    Title = query,
                    Subtitle = "Powered by Giphy",
                    Image = new ThumbnailUrl { Url = lowResImage },
                    Media = new List<MediaUrl>
                    {
                        new MediaUrl()
                        {
                            Url = image
                        }
                    }
                }.ToAttachment());
            }

            await context.PostAsync(resultMessage);
        }

        [LuisIntent("StartSalaryQuery")]
        public async Task StartSalaryQueryIntent(IDialogContext context, LuisResult result)
        {
            if (!ShouldBotReply(context)) return;
            var formDialog = new FormDialog<UserDetail>(new UserDetail(), UserDetail.BuildForm, FormOptions.PromptInStart, result.Entities);
            context.Call(formDialog, ResumeAfterSalaryQueryDialog);
        }

        private async Task ResumeAfterSalaryQueryDialog(IDialogContext context, IAwaitable<UserDetail> result)
        {
            try
            {
                var salaryQuery = await result;

                await context.PostAsync("Your PorgPowered survey has been successfully completed. You will get a confirmation email and SMS. Thanks for using PorgPowered salary bot, Welcome Again And May The Porg Be With you!!! :)");
            }
            catch(FormCanceledException ex)
            {
                string reply;
                if (ex.InnerException == null)
                {
                    reply = "It looks like you have quit the survey. Rerun this at any time by simply typing \"Start Salary Calculator\"";
                }
                else
                {
                    reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                }

                await context.PostAsync(reply);
            }
            finally
            {
                context.Done<object>(null);
            }
        }

        private async Task ShowLuisResult(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"You have reached {result.Intents[0].Intent}. You said: {result.Query}");
            context.Wait(MessageReceived);
        }

        private bool ShouldBotReply(IDialogContext context)
        {
            Activity ac = context.Activity as Activity;
            bool willReply = context.Activity.Conversation.IsGroup == true && (ac?.MentionsRecipient() == true);
            willReply = willReply || context.Activity.Conversation.IsGroup == null || context.Activity.Conversation.IsGroup == false;

            var mentions = ac.GetMentions();
            string messageText = ac.Text;
            for (int i = 0; i < mentions.Length; i++)
            {
                if (mentions[i].Mentioned.Id == ac.Recipient.Id)
                {
                    //Bot is in the @mention list.  
                    //The below example will strip the bot name out of the message, so you can parse it as if it wasn't included.  Note that the Text object will contain the full bot name, if applicable.
                    if (mentions[i].Text != null)
                        messageText = messageText.Replace(mentions[i].Text, "");
                }
            }

            return true;
        }
    }
}