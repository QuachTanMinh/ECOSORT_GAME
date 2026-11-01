using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EcoSortQuest
{
    // Display names never replace the stable API ID or split an existing player's history.
    public sealed class PlayerManager : MonoBehaviour
    {
        public static string CurrentPlayerId => LocalProgress.Load().player_id;
        public static string NameKey(string id)=>"EcoSortQuest.DisplayName.v1."+id;
        public static string DisplayName=>PlayerPrefs.GetString(NameKey(CurrentPlayerId),"");
        void Awake(){var progress=LocalProgress.Load();LocalProgress.Save(progress);}
        public static bool TryNormalizeName(string input,out string name,out string error)
        {
            name=Regex.Replace((input??"").Normalize(NormalizationForm.FormC).Trim(),@"\s+"," ");
            error="";
            if(name.Length<1 || name.Length>32){error="Tên gọi cần từ 1 đến 32 ký tự.";return false;}
            foreach(char c in name)if(!char.IsLetterOrDigit(c) && c!=' ' && c!='-' && c!='_')
            {error="Dùng chữ, số, khoảng trắng, dấu - hoặc _.";return false;}
            return true;
        }
        public static bool TrySetDisplayName(string input,out string error)
        {
            if(!TryNormalizeName(input,out var name,out error))return false;
            PlayerPrefs.SetString(NameKey(CurrentPlayerId),name);PlayerPrefs.Save();return true;
        }
    }
}
