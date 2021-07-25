using UnityEngine;

public class VectorComponent : MonoBehaviour {
	public const float ANIM_DURATION = .2f;

	public Renderer[] Renderers;

	private float _size;
	public float size { get { return _size; } set { sizeFrom = currentSize; anim = 0f; _size = value; } }

	private Color _color;
	public Color color { get { return _color; } set { _color = value; UpdateColors(); } }

	private float _currentSize;
	public float currentSize { get { return _currentSize; } private set { _currentSize = value; } }

	private float anim;
	private float sizeFrom;

	public void UpdateCurrentSize() {
		anim = Mathf.Min(ANIM_DURATION, anim + Time.deltaTime);
		float progress = anim / ANIM_DURATION;
		currentSize = sizeFrom + progress * (size - sizeFrom);
	}

	private void UpdateColors() {
		foreach (Renderer renderer in Renderers) renderer.material.color = color;
	}
}
