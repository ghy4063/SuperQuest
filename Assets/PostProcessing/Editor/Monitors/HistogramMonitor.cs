using UnityEditorInternal;
using UnityEngine;
using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    using HistogramMode = PostProcessingProfile.MonitorSettings.HistogramMode;

    public class HistogramMonitor : PostProcessingMonitor
    {
        static GUIContent s_MonitorTitle = new GUIContent("Histogram");

        ComputeShader m_ComputeShader;
        ComputeBuffer m_Buffer;
        Material m_Material;
        RenderTexture m_HistogramTexture;
        Rect m_MonitorAreaRect;

        public HistogramMonitor()
        {
			this.m_ComputeShader = EditorResources.Load<ComputeShader>("Monitors/HistogramCompute.compute");
        }

        public override void Dispose()
        {
            GraphicsUtils.Destroy(this.m_Material);
            GraphicsUtils.Destroy(this.m_HistogramTexture);

            if (this.m_Buffer != null)
				this.m_Buffer.Release();

			this.m_Material = null;
			this.m_HistogramTexture = null;
			this.m_Buffer = null;
        }

        public override bool IsSupported()
        {
            return this.m_ComputeShader != null && GraphicsUtils.supportsDX11;
        }

        public override GUIContent GetMonitorTitle()
        {
            return s_MonitorTitle;
        }

        public override void OnMonitorSettings()
        {
            EditorGUI.BeginChangeCheck();

            var refreshOnPlay = this.m_MonitorSettings.refreshOnPlay;
            var mode = this.m_MonitorSettings.histogramMode;

            refreshOnPlay = GUILayout.Toggle(refreshOnPlay, new GUIContent(FxStyles.playIcon, "Keep refreshing the histogram in play mode; this may impact performances."), FxStyles.preButton);
            mode = (HistogramMode)EditorGUILayout.EnumPopup(mode, FxStyles.preDropdown, GUILayout.MaxWidth(100f));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.m_BaseEditor.serializedObject.targetObject, "Histogram Settings Changed");
				this.m_MonitorSettings.refreshOnPlay = refreshOnPlay;
				this.m_MonitorSettings.histogramMode = mode;
                InternalEditorUtility.RepaintAllViews();
            }
        }

        public override void OnMonitorGUI(Rect r)
        {
            if (Event.current.type == EventType.Repaint)
            {
                // If m_MonitorAreaRect isn't set the preview was just opened so refresh the render to get the histogram data
                if (Mathf.Approximately(this.m_MonitorAreaRect.width, 0) && Mathf.Approximately(this.m_MonitorAreaRect.height, 0))
                    InternalEditorUtility.RepaintAllViews();

                // Sizing
                var width = this.m_HistogramTexture != null
                    ? Mathf.Min(this.m_HistogramTexture.width, r.width - 65f)
                    : r.width;
                var height = this.m_HistogramTexture != null
                    ? Mathf.Min(this.m_HistogramTexture.height, r.height - 45f)
                    : r.height;

				this.m_MonitorAreaRect = new Rect(
                        Mathf.Floor(r.x + r.width / 2f - width / 2f),
                        Mathf.Floor(r.y + r.height / 2f - height / 2f - 5f),
                        width, height
                        );

                if (this.m_HistogramTexture != null)
                {
                    Graphics.DrawTexture(this.m_MonitorAreaRect, this.m_HistogramTexture);

                    var color = Color.white;
                    const float kTickSize = 5f;

                    // Rect, lines & ticks points
                    if (this.m_MonitorSettings.histogramMode == HistogramMode.RGBSplit)
                    {
                        //  A B C D E
                        //  N       F
                        //  M       G
                        //  L K J I H

                        var A = new Vector3(this.m_MonitorAreaRect.x - 1f, this.m_MonitorAreaRect.y - 1f);
                        var E = new Vector3(A.x + this.m_MonitorAreaRect.width + 2f, this.m_MonitorAreaRect.y - 1f);
                        var H = new Vector3(E.x, E.y + this.m_MonitorAreaRect.height + 2f);
                        var L = new Vector3(A.x, H.y);

                        var N = new Vector3(A.x, A.y + (L.y - A.y) / 3f);
                        var M = new Vector3(A.x, A.y + (L.y - A.y) * 2f / 3f);
                        var F = new Vector3(E.x, E.y + (H.y - E.y) / 3f);
                        var G = new Vector3(E.x, E.y + (H.y - E.y) * 2f / 3f);

                        var C = new Vector3(A.x + (E.x - A.x) / 2f, A.y);
                        var J = new Vector3(L.x + (H.x - L.x) / 2f, L.y);

                        var B = new Vector3(A.x + (C.x - A.x) / 2f, A.y);
                        var D = new Vector3(C.x + (E.x - C.x) / 2f, C.y);
                        var I = new Vector3(J.x + (H.x - J.x) / 2f, J.y);
                        var K = new Vector3(L.x + (J.x - L.x) / 2f, L.y);

                        // Borders
                        Handles.color = color;
                        Handles.DrawLine(A, E);
                        Handles.DrawLine(E, H);
                        Handles.DrawLine(H, L);
                        Handles.DrawLine(L, new Vector3(A.x, A.y - 1f));

                        // Vertical ticks
                        Handles.DrawLine(A, new Vector3(A.x - kTickSize, A.y));
                        Handles.DrawLine(N, new Vector3(N.x - kTickSize, N.y));
                        Handles.DrawLine(M, new Vector3(M.x - kTickSize, M.y));
                        Handles.DrawLine(L, new Vector3(L.x - kTickSize, L.y));

                        Handles.DrawLine(E, new Vector3(E.x + kTickSize, E.y));
                        Handles.DrawLine(F, new Vector3(F.x + kTickSize, F.y));
                        Handles.DrawLine(G, new Vector3(G.x + kTickSize, G.y));
                        Handles.DrawLine(H, new Vector3(H.x + kTickSize, H.y));

                        // Horizontal ticks
                        Handles.DrawLine(A, new Vector3(A.x, A.y - kTickSize));
                        Handles.DrawLine(B, new Vector3(B.x, B.y - kTickSize));
                        Handles.DrawLine(C, new Vector3(C.x, C.y - kTickSize));
                        Handles.DrawLine(D, new Vector3(D.x, D.y - kTickSize));
                        Handles.DrawLine(E, new Vector3(E.x, E.y - kTickSize));

                        Handles.DrawLine(L, new Vector3(L.x, L.y + kTickSize));
                        Handles.DrawLine(K, new Vector3(K.x, K.y + kTickSize));
                        Handles.DrawLine(J, new Vector3(J.x, J.y + kTickSize));
                        Handles.DrawLine(I, new Vector3(I.x, I.y + kTickSize));
                        Handles.DrawLine(H, new Vector3(H.x, H.y + kTickSize));

                        // Separators
                        Handles.DrawLine(N, F);
                        Handles.DrawLine(M, G);

                        // Labels
                        GUI.color = color;
                        GUI.Label(new Rect(L.x - 15f, L.y + kTickSize - 4f, 30f, 30f), "0.0", FxStyles.tickStyleCenter);
                        GUI.Label(new Rect(J.x - 15f, J.y + kTickSize - 4f, 30f, 30f), "0.5", FxStyles.tickStyleCenter);
                        GUI.Label(new Rect(H.x - 15f, H.y + kTickSize - 4f, 30f, 30f), "1.0", FxStyles.tickStyleCenter);
                    }
                    else
                    {
                        //  A B C D E
                        //  P       F
                        //  O       G
                        //  N       H
                        //  M L K J I

                        var A = new Vector3(this.m_MonitorAreaRect.x, this.m_MonitorAreaRect.y);
                        var E = new Vector3(A.x + this.m_MonitorAreaRect.width + 1f, this.m_MonitorAreaRect.y);
                        var I = new Vector3(E.x, E.y + this.m_MonitorAreaRect.height + 1f);
                        var M = new Vector3(A.x, I.y);

                        var C = new Vector3(A.x + (E.x - A.x) / 2f, A.y);
                        var G = new Vector3(E.x, E.y + (I.y - E.y) / 2f);
                        var K = new Vector3(M.x + (I.x - M.x) / 2f, M.y);
                        var O = new Vector3(A.x, A.y + (M.y - A.y) / 2f);

                        var P = new Vector3(A.x, A.y + (O.y - A.y) / 2f);
                        var F = new Vector3(E.x, E.y + (G.y - E.y) / 2f);
                        var N = new Vector3(A.x, O.y + (M.y - O.y) / 2f);
                        var H = new Vector3(E.x, G.y + (I.y - G.y) / 2f);

                        var B = new Vector3(A.x + (C.x - A.x) / 2f, A.y);
                        var L = new Vector3(M.x + (K.x - M.x) / 2f, M.y);
                        var D = new Vector3(C.x + (E.x - C.x) / 2f, A.y);
                        var J = new Vector3(K.x + (I.x - K.x) / 2f, M.y);

                        // Borders
                        Handles.color = color;
                        Handles.DrawLine(A, E);
                        Handles.DrawLine(E, I);
                        Handles.DrawLine(I, M);
                        Handles.DrawLine(M, new Vector3(A.x, A.y - 1f));

                        // Vertical ticks
                        Handles.DrawLine(A, new Vector3(A.x - kTickSize, A.y));
                        Handles.DrawLine(P, new Vector3(P.x - kTickSize, P.y));
                        Handles.DrawLine(O, new Vector3(O.x - kTickSize, O.y));
                        Handles.DrawLine(N, new Vector3(N.x - kTickSize, N.y));
                        Handles.DrawLine(M, new Vector3(M.x - kTickSize, M.y));

                        Handles.DrawLine(E, new Vector3(E.x + kTickSize, E.y));
                        Handles.DrawLine(F, new Vector3(F.x + kTickSize, F.y));
                        Handles.DrawLine(G, new Vector3(G.x + kTickSize, G.y));
                        Handles.DrawLine(H, new Vector3(H.x + kTickSize, H.y));
                        Handles.DrawLine(I, new Vector3(I.x + kTickSize, I.y));

                        // Horizontal ticks
                        Handles.DrawLine(A, new Vector3(A.x, A.y - kTickSize));
                        Handles.DrawLine(B, new Vector3(B.x, B.y - kTickSize));
                        Handles.DrawLine(C, new Vector3(C.x, C.y - kTickSize));
                        Handles.DrawLine(D, new Vector3(D.x, D.y - kTickSize));
                        Handles.DrawLine(E, new Vector3(E.x, E.y - kTickSize));

                        Handles.DrawLine(M, new Vector3(M.x, M.y + kTickSize));
                        Handles.DrawLine(L, new Vector3(L.x, L.y + kTickSize));
                        Handles.DrawLine(K, new Vector3(K.x, K.y + kTickSize));
                        Handles.DrawLine(J, new Vector3(J.x, J.y + kTickSize));
                        Handles.DrawLine(I, new Vector3(I.x, I.y + kTickSize));

                        // Labels
                        GUI.color = color;
                        GUI.Label(new Rect(A.x - kTickSize - 34f, A.y - 15f, 30f, 30f), "1.0", FxStyles.tickStyleRight);
                        GUI.Label(new Rect(O.x - kTickSize - 34f, O.y - 15f, 30f, 30f), "0.5", FxStyles.tickStyleRight);
                        GUI.Label(new Rect(M.x - kTickSize - 34f, M.y - 15f, 30f, 30f), "0.0", FxStyles.tickStyleRight);

                        GUI.Label(new Rect(E.x + kTickSize + 4f, E.y - 15f, 30f, 30f), "1.0", FxStyles.tickStyleLeft);
                        GUI.Label(new Rect(G.x + kTickSize + 4f, G.y - 15f, 30f, 30f), "0.5", FxStyles.tickStyleLeft);
                        GUI.Label(new Rect(I.x + kTickSize + 4f, I.y - 15f, 30f, 30f), "0.0", FxStyles.tickStyleLeft);

                        GUI.Label(new Rect(M.x - 15f, M.y + kTickSize - 4f, 30f, 30f), "0.0", FxStyles.tickStyleCenter);
                        GUI.Label(new Rect(K.x - 15f, K.y + kTickSize - 4f, 30f, 30f), "0.5", FxStyles.tickStyleCenter);
                        GUI.Label(new Rect(I.x - 15f, I.y + kTickSize - 4f, 30f, 30f), "1.0", FxStyles.tickStyleCenter);
                    }
                }
            }
        }

        public override void OnFrameData(RenderTexture source)
        {
            if (Application.isPlaying && !this.m_MonitorSettings.refreshOnPlay)
                return;

            if (Mathf.Approximately(this.m_MonitorAreaRect.width, 0) || Mathf.Approximately(this.m_MonitorAreaRect.height, 0))
                return;

            var ratio = (float)source.width / (float)source.height;
            var h = 512;
            var w = Mathf.FloorToInt(h * ratio);

            var rt = RenderTexture.GetTemporary(w, h, 0, source.format);
            Graphics.Blit(source, rt);
            this.ComputeHistogram(rt);
			this.m_BaseEditor.Repaint();
            RenderTexture.ReleaseTemporary(rt);
        }

        void CreateBuffer(int width, int height)
        {
			this.m_Buffer = new ComputeBuffer(width * height, sizeof(uint) << 2);
        }

        void ComputeHistogram(RenderTexture source)
        {
            if (this.m_Buffer == null)
            {
                this.CreateBuffer(256, 1);
            }
            else if (this.m_Buffer.count != 256)
            {
				this.m_Buffer.Release();
                this.CreateBuffer(256, 1);
            }

            if (this.m_Material == null)
            {
				this.m_Material = new Material(Shader.Find("Hidden/Post FX/Monitors/Histogram Render")) { hideFlags = HideFlags.DontSave };
            }

            var channels = Vector4.zero;
            switch (this.m_MonitorSettings.histogramMode)
            {
                case HistogramMode.Red: channels.x = 1f; break;
                case HistogramMode.Green: channels.y = 1f; break;
                case HistogramMode.Blue: channels.z = 1f; break;
                case HistogramMode.Luminance: channels.w = 1f; break;
                default: channels = new Vector4(1f, 1f, 1f, 0f); break;
            }

            var cs = this.m_ComputeShader;

            var kernel = cs.FindKernel("KHistogramClear");
            cs.SetBuffer(kernel, "_Histogram", this.m_Buffer);
            cs.Dispatch(kernel, 1, 1, 1);

            kernel = cs.FindKernel("KHistogramGather");
            cs.SetBuffer(kernel, "_Histogram", this.m_Buffer);
            cs.SetTexture(kernel, "_Source", source);
            cs.SetInt("_IsLinear", GraphicsUtils.isLinearColorSpace ? 1 : 0);
            cs.SetVector("_Res", new Vector4(source.width, source.height, 0f, 0f));
            cs.SetVector("_Channels", channels);
            cs.Dispatch(kernel, Mathf.CeilToInt(source.width / 16f), Mathf.CeilToInt(source.height / 16f), 1);

            kernel = cs.FindKernel("KHistogramScale");
            cs.SetBuffer(kernel, "_Histogram", this.m_Buffer);
            cs.Dispatch(kernel, 1, 1, 1);

            if (this.m_HistogramTexture == null || this.m_HistogramTexture.width != source.width || this.m_HistogramTexture.height != source.height)
            {
                GraphicsUtils.Destroy(this.m_HistogramTexture);
				this.m_HistogramTexture = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

			this.m_Material.SetBuffer("_Histogram", this.m_Buffer);
			this.m_Material.SetVector("_Size", new Vector2(this.m_HistogramTexture.width, this.m_HistogramTexture.height));
			this.m_Material.SetColor("_ColorR", new Color(1f, 0f, 0f, 1f));
			this.m_Material.SetColor("_ColorG", new Color(0f, 1f, 0f, 1f));
			this.m_Material.SetColor("_ColorB", new Color(0f, 0f, 1f, 1f));
			this.m_Material.SetColor("_ColorL", new Color(1f, 1f, 1f, 1f));
			this.m_Material.SetInt("_Channel", (int)this.m_MonitorSettings.histogramMode);

            var pass = 0;
            if (this.m_MonitorSettings.histogramMode == HistogramMode.RGBMerged)
                pass = 1;
            else if (this.m_MonitorSettings.histogramMode == HistogramMode.RGBSplit)
                pass = 2;

            Graphics.Blit(null, this.m_HistogramTexture, this.m_Material, pass);
        }
    }
}
