using UnityEngine;

namespace EcoSortQuest
{
    // Explicit 2D sources: camera distance must never attenuate UI/game sounds.
    public sealed class EcoSortAudio : MonoBehaviour
    {
        public AudioSource Music { get; private set; }
        public AudioSource Effects { get; private set; }
        AudioClip music, good, retry;
        AudioClip click;
        AudioClip pickup,drop;
        public event System.Action<string> CuePlayed;
        bool generatedGood, generatedRetry;
        public static EcoSortAudio Current { get; private set; }
        public void Initialize(Camera camera, bool background)
        {
            Current=this;
            if(camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
            camera.GetComponent<AudioListener>().enabled=true;
            AudioListener.pause=false;
            Music=gameObject.AddComponent<AudioSource>();Effects=gameObject.AddComponent<AudioSource>();
            foreach(var source in new[]{Music,Effects}){source.playOnAwake=false;source.spatialBlend=0;source.ignoreListenerPause=true;source.priority=64;}
            good=Resources.Load<AudioClip>("Audio/correct");retry=Resources.Load<AudioClip>("Audio/retry");click=Resources.Load<AudioClip>("Audio/click");
            pickup=Resources.Load<AudioClip>("Audio/pickup");drop=Resources.Load<AudioClip>("Audio/drop");
            generatedGood=good==null;generatedRetry=retry==null;
            if(generatedGood)good=Tone("Correct chime",new[]{523.25f,659.25f,783.99f},.15f,.55f);
            if(generatedRetry)retry=Tone("Try again",new[]{392f,293.66f},.19f,.48f);
            if(background)
            {
                music=Tone("EcoSort gentle theme",new[]{261.63f,329.63f,392f,329.63f,293.66f,349.23f,440f,349.23f,261.63f,329.63f,392f,523.25f,440f,392f,329.63f,293.66f},.48f,.25f);
                Music.clip=music;Music.loop=true;
            }
            Apply();
        }
        public void Apply()
        {
            bool enabled=PlayerPrefs.GetInt("EcoSortQuest.Sound",1)==1;
            float master=Volume("Master",1f);
            Music.mute=Effects.mute=!enabled;
            Music.volume=master*Volume("Music",.65f);Effects.volume=master*Volume("Effects",1f);
            if(Music.clip!=null && !Music.isPlaying && enabled)Music.Play();
        }
        public static float Volume(string channel,float fallback) => Mathf.Clamp01(PlayerPrefs.GetFloat("EcoSortQuest.Volume."+channel,fallback));
        public void SetVolume(string channel,float value)
        {PlayerPrefs.SetFloat("EcoSortQuest.Volume."+channel,Mathf.Clamp01(value));PlayerPrefs.Save();Apply();}
        public void Feedback(bool correct)
        {PlayCue(correct?good:retry);}
        public void Pickup()=>PlayCue(pickup);
        public void Drop()=>PlayCue(drop);
        void PlayCue(AudioClip clip,float gain=1)
        {if(clip!=null && PlayerPrefs.GetInt("EcoSortQuest.Sound",1)==1){Effects.PlayOneShot(clip,gain);CuePlayed?.Invoke(clip.name);}}
        public void FeedbackAfterDrop(bool correct)=>StartCoroutine(DelayedFeedback(correct));
        System.Collections.IEnumerator DelayedFeedback(bool correct)
        {yield return new WaitForSecondsRealtime(.14f);Feedback(correct);}
        public void Preview()
        {
            PlayerPrefs.SetInt("EcoSortQuest.Sound",1);PlayerPrefs.Save();
            AudioListener.volume=1;AudioListener.pause=false;
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.audioMasterMute=false;
            #endif
            Apply();Feedback(true);
        }
        public void Click()
        {PlayCue(click,.55f);}
        static AudioClip Tone(string name,float[] notes,float duration,float gain)
        {
            const int rate=44100;int size=Mathf.RoundToInt(duration*rate);
            float[] samples=new float[size*notes.Length];
            for(int n=0;n<notes.Length;n++)for(int i=0;i<size;i++)
            {
                float t=(float)i/rate, progress=(float)i/size;
                float envelope=Mathf.Min(1,t/.012f)*Mathf.Pow(1-progress,1.8f);
                samples[n*size+i]=gain*envelope*(.8f*Mathf.Sin(2*Mathf.PI*notes[n]*t)+.2f*Mathf.Sin(4*Mathf.PI*notes[n]*t));
            }
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy(){if(Current==this)Current=null;if(music!=null)Destroy(music);if(generatedGood && good!=null)Destroy(good);if(generatedRetry && retry!=null)Destroy(retry);}
    }
}
