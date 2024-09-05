using BlazorApp1.Data;
using Blazored.TextEditor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System.Net;

namespace BlazorApp1.Components.Pages
{

    public partial class Editor
    {
        [Parameter]
        public string? condition { get; set; }
        BlazoredTextEditor QuillHtml;
        string QuillHTMLContent;
        string text = "";
        private async void OnSubmitCreate()
        {
            //Get Text from Texteditor
            string temp = await QuillHtml.GetText();
            StringService.EditorContent = temp;


            NavManager.NavigateTo($"/ImageCreation");

        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (condition == "Back")
                {
                    //Wenn die Seite mit dem Back Parameter aufgerufen wurde, lade vorher eingegeben Text
                    text = WebUtility.HtmlEncode(StringService.EditorContent).Replace("\n", "<br />").Replace("\r\n", "<br />");

                    await InvokeAsync(StateHasChanged);
                }
                else if (condition == "Result")
                {
                    //Wenn die Seite mit dem Result Parameter aufgerufen wurde, lade den Ausgabetext der KI
                    text = StringService.Answer;
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    //Wenn die Seite initialisiert wird, lasse den Text leer
                }
            }

        }

    }
}