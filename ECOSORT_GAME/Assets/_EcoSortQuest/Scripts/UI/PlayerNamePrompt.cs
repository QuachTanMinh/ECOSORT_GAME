using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    public sealed class PlayerNamePrompt:MonoBehaviour
    {
        ViewFactory view;Action saved;RectTransform panel;
        public void Configure(ViewFactory factory,Action onSaved){view=factory;saved=onSaved;}
        public bool IsOpen=>panel!=null;
        public void Show()
        {
            if(view==null || panel!=null)return;
            panel=view.ModalCard(.27f,.28f,.46f,.43f,"Player name prompt");
            view.Text(panel,"MÌNH GỌI BẠN LÀ GÌ NHỈ?",.06f,.77f,.88f,.14f,29);
            view.Text(panel,"Dùng biệt danh là được, không cần tên thật. Bạn đổi tên lúc nào cũng không mất tiến trình.",.06f,.56f,.88f,.17f,22,ViewFactory.Muted);
            var input=view.Input(panel,"Tên gọi của bạn…",.06f,.36f,.88f,.13f);input.name="Player nickname";input.characterLimit=32;input.text=PlayerManager.DisplayName;
            var note=view.Text(panel,"",.06f,.24f,.88f,.10f,20,ViewFactory.Muted);
            void Submit(string value)
            {
                if(!PlayerManager.TrySetDisplayName(value,out var error)){note.text=error;return;}
                Destroy(panel.gameObject);panel=null;saved?.Invoke();
            }
            var button=view.Button(panel,"Lưu tên",.06f,.07f,.52f,.12f,()=>Submit(input.text),true);
            view.Button(panel,"Chơi ngay",.63f,.07f,.31f,.12f,()=>Submit("Bạn nhỏ"));
            void Validate(string value){button.interactable=PlayerManager.TryNormalizeName(value,out _,out var error);note.text=string.IsNullOrWhiteSpace(value)?"":error;}
            input.onValueChanged.AddListener(Validate);Validate(input.text);input.ActivateInputField();
        }
    }
}
