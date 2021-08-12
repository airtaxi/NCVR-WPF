using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace NCVR_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CbxNative_Checked(object sender, RoutedEventArgs e)
        {
            if(CbxForeign != null)
                CbxForeign.IsChecked = false;
        }
        private void CbxNative_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CbxNative != null && CbxForeign.IsChecked == false)
                CbxNative.IsChecked = true;
        }

        private void CbxForeign_Checked(object sender, RoutedEventArgs e)
        {
            if (CbxNative != null)
                CbxNative.IsChecked = false;
        }

        private void CbxForeign_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CbxForeign != null && CbxNative.IsChecked == false)
                CbxForeign.IsChecked = true;
        }

        private void CbxMale_Checked(object sender, RoutedEventArgs e)
        {
            if (CbxFemale != null)
                CbxFemale.IsChecked = false;
        }

        private void CbxMale_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CbxMale != null && CbxFemale.IsChecked == false)
                CbxMale.IsChecked = true;
        }

        private void CbxFemale_Checked(object sender, RoutedEventArgs e)
        {
            if (CbxMale != null)
                CbxMale.IsChecked = false;
        }

        private void CbxFemale_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CbxFemale != null && CbxMale.IsChecked == false)
                CbxFemale.IsChecked = true;
        }

        private RestRequest GenerateRequest(string data)
        {
            RestRequest request = new RestRequest();
            request.Method = Method.POST;
            request.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddOrUpdateHeader("charset", "UTF-8");
            request.AddParameter("application/x-www-form-urlencoded", data, ParameterType.RequestBody);
            return request;
        }

        private async void BtApply_Click(object sender, RoutedEventArgs e)
        {
            BtApply.IsEnabled = false;

            var name = TbxName.Text;
            var birthdayDate = CdpBirthday.SelectedDate.GetValueOrDefault();
            var birthday = $"{birthdayDate.Year}{birthdayDate.Month:D2}{birthdayDate.Day:D2}";
            var ntvFrnrCd = CbxNative.IsChecked == true ? "L" : "F";
            var sexCd = CbxMale.IsChecked == true ? "M" : "F";
            var telComCd = (CmbTelecom.SelectedIndex + 1).ToString("00");
            var telNo = $"{TbxTel1.Text}{TbxTel2.Text}{TbxTel3.Text}";
            var agree5 = "";
            if (CmbTelecom.SelectedIndex >= 4)
                agree5 = "&agree5=Y";
            string data = "svcGb=P&name=" + name + "&birthday=" + birthday + "&sexCd=" + sexCd + "&ntvFrnrCd=" + ntvFrnrCd + "&telComCd=" + telComCd + "&telNo=" + telNo + $"&agree1=Y&agree2=Y&agree3=Y&agree4=Y" + agree5;

            RestClient client = new RestClient("https://ncvr2.kdca.go.kr/svc/kcb/callKcb");
            RestRequest request = GenerateRequest(data);
            var response = await client.ExecuteAsync(request);
            var responseJson = JObject.Parse(response.Content);
            var responseCode = responseJson.Value<string>("rsltCd");
            if (responseCode == "B000")
            {
                var txSeqNo = responseJson.Value<string>("txSeqNo");
                var otpNo = await this.ShowInputAsync("안내", "인증 번호를 입력해주세요.");
                data = "svcGb=R&txSeqNo=" + txSeqNo + "&telNo=" + telNo + "&otpNo=" + otpNo;
                request = GenerateRequest(data);
                response = await client.ExecuteAsync(request);
                responseJson = JObject.Parse(response.Content);
                responseCode = responseJson.Value<string>("rsltCd");
                var reqId = responseJson.Value<string>("reqId");
                string url = "https://ncvr2.kdca.go.kr/svc/waiting?reqId=" + reqId;
                if (responseCode == "CONFLICT")
                    await this.ShowMessageAsync("오류", "동일인은 한번 접속된 후 10분 이후에 접속이 가능합니다.");
                else if (responseCode == "B000")
                {
                    var width = Width;
                    Width = 880;
                    if ((await this.ShowMessageAsync("안내", $"본인인증이 완료되었습니다.\n" + "접속 후 페이지 오류 또는 실수로 탭을 닫은 경우 https://ncvr2.kdca.go.kr/svc/waiting?reqId=" + reqId + "로 접속하시기 바랍니다.\n본 주소로 접속 시 10분 동안 재인증 대기를 하실 필요가 없습니다.\n주소를 클립보드에 복사하시겠습니까?", MessageDialogStyle.AffirmativeAndNegative)) == MessageDialogResult.Affirmative)
                    {
                        Width = width;
                        Clipboard.SetDataObject(url);
                        await this.ShowMessageAsync("안내", "클립보드 복사가 완료되었습니다.\n본 메시지창을 닫으면 웹 페이지가 열립니다.");
                    }
                    else
                        await this.ShowMessageAsync("안내", "본 메시지창을 닫으면 신청 웹 페이지가 열립니다.");
                }
                else
                    await this.ShowMessageAsync("오류", $"{responseJson.Value<string>("rsltMsg")}");
                if (!string.IsNullOrEmpty(url))
                    System.Diagnostics.Process.Start(url);
            }
            else
            {
                await this.ShowMessageAsync("오류", $"{responseJson.Value<string>("rsltMsg")}");
            }
            BtApply.IsEnabled = true;
        }
    }
}
