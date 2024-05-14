using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TKPackages.DrawMesh.Runtime
{

  [RequireComponent(typeof(Material))]
  [RequireComponent(typeof(MeshRenderer))]
  [RequireComponent(typeof(MeshCollider))]
  public class DynamicPaintObject : MonoBehaviour
  {
    #region SerializedProperties

    [SerializeField, Tooltip("メインテクスチャのプロパティ名")]
    private string mainTextureName = "_MainTex";

    [SerializeField, Tooltip("バンプマップテクスチャのプロパティ名")]
    private string bumpTextureName = "_BumpMap";

    [SerializeField, Tooltip("テクスチャペイント用マテリアル")]
    private Material paintMaterial = null;

    [SerializeField, Tooltip("ブラシ用テクスチャ")] private Texture2D blush = null;

    [SerializeField, Range(0, 1), Tooltip("ブラシの大きさ")]
    private float blushScale = 0.1f;

    [SerializeField, Tooltip("ブラシの色")] private Color blushColor = default(Color);

    [SerializeField, Tooltip("ブラシバンプマップ用マテリアル")]
    private Material paintBumpMaterial = null;

    [SerializeField, Tooltip("ブラシバンプマップ用テクスチャ")]
    private Texture2D blushBump = null;

    [SerializeField, Range(0, 1), Tooltip("バンプマップブレンド比率")]
    private float bumpBlend = 1;

    #endregion SerializedProperties

    #region ShaderPropertyID

    private int mainTexturePropertyID;
    private int bumpTexturePropertyID;
    private int paintUVPropertyID;
    private int blushTexturePropertyID;
    private int blushScalePropertyID;
    private int blushColorPropertyID;
    private int blushBumpTexturePropertyID;
    private int blushBumpBlendPropertyID;

    #endregion ShaderPropertyID

    private RenderTexture paintTexture;
    private RenderTexture paintBumpTexture;
    private Material material;

    /// <summary>
    /// ブラシの大きさ
    /// [0,1]の範囲をとるテクスチャサイズの比
    /// </summary>
    public float BlushScale
    {
      get { return Mathf.Clamp01(blushScale); }
      set { blushScale = Mathf.Clamp01(value); }
    }

    public float BumpBlend
    {
      get { return Mathf.Clamp01(bumpBlend); }
      set { bumpBlend = Mathf.Clamp01(value); }
    }

    /// <summary>
    /// ブラシの色
    /// </summary>
    public Color BlushColor
    {
      get { return blushColor; }
      set { blushColor = value; }
    }

    /// <summary>
    /// ブラシのテクスチャ
    /// </summary>
    public Texture BlushTexture
    {
      get { return blush; }
      set { blush = (Texture2D)value; }
    }

    /// <summary>
    /// ブラシのバンプマップテクスチャ
    /// </summary>
    public Texture BlushBumpTexture
    {
      get { return blushBump; }
      set { blushBump = (Texture2D)value; }
    }

    #region UnityEventMethod

    public void Awake()
    {
      InitPropertyID();

      var meshRenderer = GetComponent<MeshRenderer>();
      material = meshRenderer.material;
      var mainTexture = material.GetTexture(mainTexturePropertyID);
      var bumpTexture = material.GetTexture(bumpTexturePropertyID);

      //Textureがもともとついてないオブジェクトには非対応
      if (mainTexture == null)
      {
        Debug.LogWarning("[DynamicPaintObject] : テクスチャの設定されていないオブジェクトに適用することはできません");
        Destroy(this);
        return;
      }
      else
      {
        //DynamicPaint用RenderTextureの生成
        paintTexture = new RenderTexture(mainTexture.width, mainTexture.height, 0, RenderTextureFormat.ARGB32,
          RenderTextureReadWrite.Default);
        //メインテクスチャのコピー
        Graphics.Blit(mainTexture, paintTexture);
        //マテリアルのテクスチャをRenderTextureに変更
        material.SetTexture(mainTexturePropertyID, paintTexture); //TODO:_MainTexではなく、テクスチャを指定できるように(あと文字列やめたい)
      }

      if (blushBump != null && bumpTexture == null)
      {
        Debug.LogWarning("[DynamicPaintObject] : バンプマップテクスチャの設定されていないオブジェクトに適用することはできません");
        Destroy(this);
        return;
      }

      {
        //TODO:法線マップテクスチャの生成(インスペクターからこの機能を使うか任意にするべき)
        paintBumpTexture = new RenderTexture(mainTexture.width, mainTexture.height, 0, RenderTextureFormat.ARGB32,
          RenderTextureReadWrite.Default);
        //法線マップのコピー
        Graphics.Blit(bumpTexture, paintBumpTexture);
        //マテリアルの法線マップテクスチャをRenderTextureに変更
        material.SetTexture(bumpTexturePropertyID, paintBumpTexture);
      }
    }

#if UNITY_EDITOR && DEBUG_DYNAMIC_TEXTURE_PAINT
  public void OnGUI()
  {
    GUILayout.Label("Main Shader: " + paintMaterial.shader.name);
    GUILayout.Label("Bump Shader: " + paintBumpMaterial.shader.name);
    GUILayout.Label("Blush Main: " + (blush != null).ToString());
    GUILayout.Label("Blush Bump: " + (blushBump != null).ToString());
    GUILayout.Label("Support Main: " + paintMaterial.shader.isSupported);
    GUILayout.Label("Support Bump: " + paintBumpMaterial.shader.isSupported);
    GUILayout.Label("RenderTexture Main: " + (paintTexture != null).ToString());
    GUILayout.Label("RenderTexture Bump: " + (paintBumpTexture != null).ToString());
    GUILayout.Label("Main Texture ID:" + mainTexturePropertyID);
    GUILayout.Label("Bump Texture ID:" + bumpTexturePropertyID);
    GUILayout.Label("Paint UV ID:" + paintUVPropertyID);
    GUILayout.Label("Blush Main Texture ID:" + blushTexturePropertyID);
    GUILayout.Label("Blush Bump Texture ID:" + blushBumpTexturePropertyID);
    GUILayout.Label("Blush Scale ID:" + blushScalePropertyID);
    GUILayout.Label("Blush Color ID:" + blushColorPropertyID);
    GUILayout.Label("Blush Bump Blend ID::" + blushBumpBlendPropertyID);
  }
#endif

    #endregion UnityEventMethod

    /// <summary>
    /// シェーダーのプロパティIDを初期化する
    /// </summary>
    private void InitPropertyID()
    {
      mainTexturePropertyID = Shader.PropertyToID(mainTextureName);
      bumpTexturePropertyID = Shader.PropertyToID(bumpTextureName);

      paintUVPropertyID = Shader.PropertyToID("_PaintUV");
      blushTexturePropertyID = Shader.PropertyToID("_Blush");
      blushScalePropertyID = Shader.PropertyToID("_BlushScale");
      blushColorPropertyID = Shader.PropertyToID("_BlushColor");
      blushBumpTexturePropertyID = Shader.PropertyToID("_BlushBump");
      blushBumpBlendPropertyID = Shader.PropertyToID("_BumpBlend");
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blush">ブラシテクスチャ</param>
    /// <param name="blushColor">ブラシカラー</param>
    /// <param name="blushScale">ブラシの大きさ(UV座病スケール値[0,1])</param>
    /// <param name="blushBump">ブラシのバンプマップテクスチャ</param>
    /// <param name="bumpBlend">バンプマップテクスチャブレンド(ブレンド率[0,1])</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Texture blush, Color blushColor, float blushScale, Texture blushBump,
      float bumpBlend)
    {
      if (hitInfo.collider != null && hitInfo.collider.gameObject == gameObject)
      {
        var uv = hitInfo.textureCoord;
        RenderTexture buf = RenderTexture.GetTemporary(paintTexture.width, paintTexture.height);

        #region ErrorCheck

        if (buf == null)
        {
          Debug.LogError("テンポラリテクスチャの生成に失敗しました。");
          return false;
        }

        if (blush == null)
        {
          Debug.LogError("ブラシが設定されていません。値がNULLです。");
          return false;
        }

        if (paintMaterial == null)
        {
          Debug.LogError("ブラシ用のマテリアルが設定されていません。値がNULLです。");
          return false;
        }

        #endregion ErrorCheck

        #region ParameterDeform

        blushScale = Mathf.Clamp01(blushScale);
        bumpBlend = Mathf.Clamp01(bumpBlend);

        #endregion ParameterDeform

        //メインテクスチャへのペイント
        paintMaterial.SetVector(paintUVPropertyID, uv);
        paintMaterial.SetTexture(blushTexturePropertyID, blush);
        paintMaterial.SetFloat(blushScalePropertyID, blushScale);
        paintMaterial.SetVector(blushColorPropertyID, blushColor);
        Graphics.Blit(paintTexture, buf, paintMaterial);
        Graphics.Blit(buf, paintTexture);

        //バンプマップへのペイント

        #region LogWarningBumpMap

        //オブジェクト生成時にBumpMapテクスチャが設定されていなかったが、メソッドにブラシバンプマップテクスチャが渡された
        if (blushBump != null && paintBumpTexture == null)
        {
          Debug.LogWarning("初期化時にバンプマップテクスチャを生成していません。バンプマップ処理をスキップします。");
        }

        #endregion LogWarningBumpMap

        if (blushBump != null && paintBumpTexture != null)
        {
          paintBumpMaterial.SetVector(paintUVPropertyID, uv);
          paintBumpMaterial.SetTexture(blushTexturePropertyID, blush);
          paintBumpMaterial.SetTexture(blushBumpTexturePropertyID, blushBump);
          paintBumpMaterial.SetFloat(blushScalePropertyID, blushScale);
          paintBumpMaterial.SetFloat(blushBumpBlendPropertyID, bumpBlend);
          Graphics.Blit(paintBumpTexture, buf, paintBumpMaterial);
          Graphics.Blit(buf, paintBumpTexture);
        }

        RenderTexture.ReleaseTemporary(buf);
        return true;
      }

      return false;
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blush">ブラシテクスチャ</param>
    /// <param name="blushColor">ブラシカラー</param>
    /// <param name="blushBump">ブラシのバンプマップテクスチャ</param>
    /// <param name="bumpBlend">バンプマップテクスチャブレンド(ブレンド率[0,1])</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Texture blush, Color blushColor, Texture blushBump, float bumpBlend)
    {
      return Paint(hitInfo, blush, blushColor, BlushScale, blushBump, bumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blush">ブラシテクスチャ</param>
    /// <param name="blushBump">ブラシのバンプマップテクスチャ</param>
    /// <param name="bumpBlend">バンプマップテクスチャブレンド(ブレンド率[0,1])</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Texture blush, Texture blushBump, float bumpBlend)
    {
      return Paint(hitInfo, blush, BlushColor, BlushScale, blushBump, bumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blush">ブラシテクスチャ</param>
    /// <param name="blushBump">ブラシのバンプマップテクスチャ</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Texture blush, Texture blushBump)
    {
      return Paint(hitInfo, blush, BlushColor, BlushScale, blushBump, BumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blushBump">ブラシのバンプマップテクスチャ</param>
    /// <param name="bumpBlend">バンプマップテクスチャブレンド(ブレンド率[0,1])</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Texture blushBump, float bumpBlend)
    {
      return Paint(hitInfo, BlushTexture, BlushColor, BlushScale, blushBump, bumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blush">ブラシテクスチャ</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Texture blush)
    {
      return Paint(hitInfo, blush, BlushColor, BlushScale, BlushBumpTexture, BumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blushColor">ブラシカラー</param>
    /// <param name="blushScale">ブラシの大きさ(UV座病スケール値[0,1])</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Color blushColor, float blushScale)
    {
      return Paint(hitInfo, BlushTexture, blushColor, blushScale, BlushBumpTexture, BumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blushColor">ブラシカラー</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Color blushColor)
    {
      return Paint(hitInfo, BlushTexture, blushColor, BlushScale, BlushBumpTexture, BumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blushScale">ブラシの大きさ(UV座病スケール値[0,1])</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, float blushScale)
    {
      return Paint(hitInfo, BlushTexture, BlushColor, blushScale, BlushBumpTexture, BumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blush">ブラシテクスチャ</param>
    /// <param name="blushColor">ブラシカラー</param>
    /// <param name="blushScale">ブラシの大きさ(UV座病スケール値[0,1])</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Texture blush, Color blushColor, float blushScale)
    {
      return Paint(hitInfo, blush, blushColor, blushScale, BlushBumpTexture, BumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <param name="blush">ブラシテクスチャ</param>
    /// <param name="blushColor">ブラシカラー</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo, Texture blush, Color blushColor)
    {
      return Paint(hitInfo, blush, blushColor, BlushScale, BlushBumpTexture, BumpBlend);
    }

    /// <summary>
    /// ペイント処理
    /// </summary>
    /// <param name="hitInfo">RaycastのHit情報</param>
    /// <returns>ペイントの成否</returns>
    public bool Paint(RaycastHit hitInfo)
    {
      return Paint(hitInfo, BlushTexture, BlushColor, BlushScale, BlushBumpTexture, BumpBlend);
    }
  }
}