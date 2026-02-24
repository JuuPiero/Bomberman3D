using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ThanhHoang.Bomberman
{
    public class ScreenUI : BaseScreen
    {
        protected CanvasRenderer canvasRenderer;

        //void Start()
        //{
        //    canvasRenderer.SetAlpha(1.0f);
        //}


        protected virtual void FadeIn()
        {
            canvasRenderer.SetAlpha(1.0f);
        }

    }
}
