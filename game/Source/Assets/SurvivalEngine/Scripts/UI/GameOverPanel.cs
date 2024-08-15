using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{
    public class GameOverPanel : UISlotPanel
    {
        private static GameOverPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();

        }

        protected override void Update()
        {
            base.Update();

        }

        public void OnClickLoad()
        {
            if (PlayerData.HasLastSave())
                StartCoroutine(LoadRoutine());
            else
                StartCoroutine(NewRoutine());
        }

        public void OnClickNew()
        {
            StartCoroutine(NewRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            BlackPanel.Get().Show();

            yield return new WaitForSeconds(1f);

            TheGame.Load();
        }

        private IEnumerator NewRoutine()
        {
            BlackPanel.Get().Show();

            yield return new WaitForSeconds(1f);

            TheGame.NewGame();
        }

        public static GameOverPanel Get()
        {
            return _instance;
        }
    }

}
