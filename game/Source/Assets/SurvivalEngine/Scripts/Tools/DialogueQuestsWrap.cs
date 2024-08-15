using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if DIALOGUE_QUESTS
using DialogueQuests;
#endif

namespace SurvivalEngine
{
    /// <summary>
    /// Wrapper class for integrating DialogueQuests
    /// </summary>

    public class DialogueQuestsWrap : MonoBehaviour
    {

#if DIALOGUE_QUESTS

        private HashSet<Actor> inited_actors = new HashSet<Actor>();
        private float timer = 1f;

        static DialogueQuestsWrap()
        {
            TheGame.afterLoad += ReloadDQ;
            TheGame.afterNewGame += NewDQ;
        }

        void Awake()
        {
            PlayerData.LoadLast(); //Make sure the game is loaded

            TheGame the_game = FindObjectOfType<TheGame>();
            NarrativeManager narrative = FindObjectOfType<NarrativeManager>();

            if (narrative != null)
            {
                narrative.onPauseGameplay += OnPauseGameplay;
                narrative.onUnpauseGameplay += OnUnpauseGameplay;
                narrative.onPlaySFX += OnPlaySFX;
                narrative.onPlayMusic += OnPlayMusic;
                narrative.onStopMusic += OnStopMusic;
                narrative.getTimestamp += GetTimestamp;
                narrative.use_custom_audio = true;
            }
            else
            {
                Debug.LogError("Dialogue Quests: Integration failed - Make sure to add the DQManager to the scene");
            }

            if (the_game != null)
            {
                the_game.beforeSave += SaveDQ;
                LoadDQ();
            }
        }

        private void Start()
        {
            Actor player = Actor.GetPlayerActor();
            if (player == null)
            {
                Debug.LogError("Dialogue Quests: Integration failed - Make sure to add the Actor script on the PlayerCharacter, with an ActorData that has is_player to true ");
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer > 1f)
            {
                timer = 0f;
                SlowUpdate();
            }
        }

        private void SlowUpdate()
        {
            foreach (Actor actor in Actor.GetAll())
            {
                if (!inited_actors.Contains(actor))
                {
                    inited_actors.Add(actor);
                    InitActor(actor);
                }
            }
        }

        private void InitActor(Actor actor)
        {
            if (actor != null)
            {
                Selectable select = actor.GetComponent<Selectable>();
                if (select != null)
                {
                    actor.auto_interact_enabled = false;
                    select.onUse += (PlayerCharacter character) =>
                    {
                        character.StopMove();
                        character.FaceTorward(actor.transform.position);
                        actor.Interact(character.GetComponent<Actor>());
                    };
                }
            }
        }

        //Dont this one call during awake (before NarrativeManager.Get() wont work)
        private static void ReloadDQ()
        {
            NarrativeData.Unload();
            LoadDQ();
        }

        private static void NewDQ()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                NarrativeData.Unload();
                NarrativeData.NewGame(pdata.filename);
            }
        }

        private static void LoadDQ()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                NarrativeData.AutoLoad(pdata.filename);
            }
        }

        private void SaveDQ(string filename)
        {
            if (NarrativeData.Get() != null && !string.IsNullOrEmpty(filename))
            {
                NarrativeData.Save(filename, NarrativeData.Get());
            }
        }

        private void OnPauseGameplay()
        {
            TheGame.Get().PauseScripts();
        }

        private void OnUnpauseGameplay()
        {
            TheGame.Get().UnpauseScripts();
        }

        private void OnPlaySFX(string channel, AudioClip clip, float vol = 0.8f)
        {
            TheAudio.Get().PlaySFX(channel, clip, vol);
        }

        private void OnPlayMusic(string channel, AudioClip clip, float vol = 0.4f)
        {
            TheAudio.Get().PlayMusic(channel, clip, vol);
        }

        private void OnStopMusic(string channel)
        {
            TheAudio.Get().StopMusic(channel);
        }
		
		private float GetTimestamp()
        {
            return TheGame.Get().GetTimestamp();
        }

#endif

    }
}

