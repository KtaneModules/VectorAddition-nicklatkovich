using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class VectorAdditionModule : MonoBehaviour {
	public const bool FORCE_EXTRA_TIME_ADDITION = false;
	public const float TWITCH_CYCLE_INTERVAL = 3f;
	public const float TWITCH_NEEDY_EXTRA_TIME = 80f;
	private static readonly Color[] COLORS = { Color.red, Color.blue, Color.magenta, Color.yellow, Color.cyan };

	private static int moduleIdCounter = 1;

	public readonly string TwitchHelpMessage = new[] {
		"\"{0} left\" or \"{0} right\" - move left/right",
		"\"{0} submit\" - submit current selected vector",
		"\"{0} cycle\" - cycle all vectors",
		"\"{0} submit 1 - submit vector by its index\"",
		"\"{0} submit (1;-1)\" - submit vector by its value. Top is (0;3), right is (3;0)"
	}.Join(" | ");

	public GameObject VectorsContainer;
	public Renderer BackgroundRenderer;
	public TextMesh IndexText;
	public TextMesh LengthText;
	public KMNeedyModule Needy;
	public KMAudio Audio;
	public KMSelectable LeftButton;
	public KMSelectable RightButton;
	public KMSelectable SubmitButton;
	public VectorComponent VectorPrefab;

	public bool TwitchPlaysActive;

	private bool onceActivated = false;
	private bool activated = false;
	private int moduleId;
	private int index = 0;
	private float activationTime;
	private Vector2Int expectedAnswer;
	private Vector2Int[] vectors;
	private Color[] colors;
	private VectorComponent[] vectorComponents;

	private void Start() {
		moduleId = moduleIdCounter++;
		Needy.OnActivate += OnActivate;
	}

	private void Update() {
		Color warningColor = Color.green;
		if (!onceActivated) warningColor = new Color(1f, Mathf.Sin(Time.time * Mathf.PI) * 0.5f + 0.5f, 0f);
		if (activated) {
			warningColor = new Color(1f, Mathf.Sin(Mathf.PI * (activationTime + Mathf.Pow(Time.time - activationTime, 1.2f))) * 0.5f + 0.5f, 0f);
			for (int i = 0; i < vectors.Length; i++) {
				vectorComponents[i].UpdateCurrentSize();
				vectorComponents[i].transform.localScale = Vector3.one * vectors[i].magnitude * vectorComponents[i].currentSize;
			}
		}
		BackgroundRenderer.material.SetColor("_UnlitTint", warningColor);
	}

	private void OnActivate() {
		Needy.OnNeedyActivation += OnNeedyActivation;
		Needy.OnNeedyDeactivation += OnNeedyDeactivation;
		Needy.OnTimerExpired += OnTimerExpired;
		LeftButton.OnInteract += () => { OnLeftButtonPressed(); return false; };
		RightButton.OnInteract += () => { OnRightButtonPressed(); return false; };
		SubmitButton.OnInteract += () => { OnSubmitPressed(); return false; };
	}

	private void OnSubmitPressed() {
		if (vectors[index] == expectedAnswer) {
			Debug.LogFormat("[Vector Addition #{0}] Module deactivated. Used time: {1}", moduleId, Time.time - activationTime);
			Needy.HandlePass();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
			OnNeedyDeactivation();
		} else {
			Debug.LogFormat("[Vector Addition #{0}] Invalid vector submitted: {1}", moduleId, string.Format("({0};{1})", vectors[index].x, vectors[index].y));
			Needy.HandleStrike();
		}
	}

	private void OnLeftButtonPressed() {
		if (!activated) return;
		if (index == 0) return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		vectorComponents[index].size = 0f;
		index -= 1;
		vectorComponents[index].size = 1f;
		IndexText.text = (index + 1).ToString();
	}

	private void OnRightButtonPressed() {
		if (!activated) return;
		if (index == vectorComponents.Length - 1) return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		vectorComponents[index].size = 0f;
		index += 1;
		vectorComponents[index].size = 1f;
		IndexText.text = (index + 1).ToString();
	}

	private void OnNeedyActivation() {
		if (TwitchPlaysActive || FORCE_EXTRA_TIME_ADDITION) Needy.SetNeedyTimeRemaining(Needy.GetNeedyTimeRemaining() + TWITCH_NEEDY_EXTRA_TIME);
		onceActivated = true;
		activated = true;
		activationTime = Time.time;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < 3; i++) list.Add(GetRandomNonZeroVector());
		Vector2Int sum = list.Aggregate(Vector2Int.zero, (res, v) => res + v);
		while (sum != Vector2Int.zero) {
			int dx = sum.x == 0 ? 0 : Random.Range(1, Mathf.Min(3, Mathf.Abs(sum.x)));
			if (sum.x > 0) dx = -dx;
			int dy = sum.y == 0 ? 0 : Random.Range(1, Mathf.Min(3, Mathf.Abs(sum.y)));
			if (sum.y > 0) dy = -dy;
			Vector2Int v = new Vector2Int(dx, dy);
			list.Add(v);
			sum += v;
		}
		expectedAnswer = GetRandomNonZeroVector();
		list.Add(expectedAnswer);
		vectors = list.ToArray().Shuffle();
		Debug.LogFormat("[Vector Addition #{0}] Vectors: {1}", moduleId, vectors.Select(v => string.Format("({0};{1})", v.x, v.y)).Join(", "));
		Debug.LogFormat("[Vector Addition #{0}] Expected answer: {1}", moduleId, string.Format("({0};{1})", expectedAnswer.x, expectedAnswer.y));
		vectorComponents = vectors.Select(v => {
			VectorComponent res = Instantiate(VectorPrefab);
			res.transform.parent = VectorsContainer.transform;
			res.transform.localPosition = Vector3.zero;
			res.transform.localRotation = Quaternion.LookRotation(new Vector3(-v.y, 0, v.x), Vector3.up);
			res.transform.localScale = Vector3.zero;
			res.size = 0f;
			res.color = COLORS.PickRandom();
			return res;
		}).ToArray();
		vectorComponents[0].size = 1f;
		IndexText.text = "1";
		LengthText.text = vectorComponents.Length.ToString();
		index = 0;
	}

	private void OnNeedyDeactivation() {
		if (!activated) return;
		activated = false;
		foreach (VectorComponent vectorComponent in vectorComponents) Destroy(vectorComponent.gameObject);
		IndexText.text = "?";
		LengthText.text = "?";
	}

	private void OnTimerExpired() {
		Needy.HandleStrike();
		Debug.LogFormat("[Vector Addition #{0}] Timer expired", moduleId);
		OnNeedyDeactivation();
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		if (!activated) yield break;
		command = command.Trim().ToLower();
		if (command == "left") {
			yield return null;
			yield return new[] { LeftButton };
			yield break;
		}
		if (command == "right") {
			yield return null;
			yield return new[] { RightButton };
			yield break;
		}
		if (command == "submit") {
			yield return null;
			yield return new[] { SubmitButton };
			yield break;
		}
		if (command == "cycle") {
			yield return null;
			if (index > 0) yield return Enumerable.Range(0, index).Select(_ => LeftButton).ToArray();
			yield return new WaitForSeconds(TWITCH_CYCLE_INTERVAL);
			for (int i = 1; i < vectorComponents.Length; i++) {
				if (!activated) yield break;
				yield return new[] { RightButton };
				yield return new WaitForSeconds(TWITCH_CYCLE_INTERVAL);
			}
			yield return Enumerable.Range(0, index).Select(_ => LeftButton).ToArray();
			yield break;
		}
		if (Regex.IsMatch(command, @"submit +[1-9]\d?")) {
			yield return null;
			int index = int.Parse(command.Split(' ').Last());
			if (index > vectorComponents.Length) {
				yield return "sendtochat {0}, !{1} Invalid index " + index;
				yield break;
			}
			if (index != this.index) {
				KMSelectable button = index < this.index ? LeftButton : RightButton;
				int diff = Mathf.Abs(index - this.index);
				yield return Enumerable.Range(0, diff).Select(_ => button).ToArray();
			}
			yield return new[] { SubmitButton };
			yield break;
		}
		if (Regex.IsMatch(command, @"submit +\( *-?[0-3] *; *-?[0-3] *\)")) {
			yield return null;
			command = command.Split(' ').Skip(1).Where(s => s.Length > 0).Join("");
			command = command.Skip(1).Take(command.Length - 2).Join("");
			Debug.Log(command);
			int[] dims = command.Split(';').Select(s => int.Parse(s.Trim())).ToArray();
			Vector2Int vectorToFind = new Vector2Int(dims[0], dims[1]);
			if (index > 0) yield return Enumerable.Range(0, index).Select(_ => LeftButton).ToArray();
			while (true) {
				if (!activated) yield break;
				if (vectors[index] == vectorToFind) {
					yield return new[] { SubmitButton };
					yield break;
				}
				if (index == vectors.Length - 1) break;
				yield return new[] { RightButton };
			}
			yield return "sendtochat {0}, !{1} " + string.Format("Vector ({0};{1}) not found", vectorToFind.x, vectorToFind.y);
			yield break;
		}
		yield break;
	}

	private IEnumerator TwitchHandleForcedSolve() {
		yield return null;
	}

	private Vector2Int GetRandomNonZeroVector() {
		int r = Random.Range(0, 48);
		if (r >= 24) r += 1;
		return new Vector2Int(r % 7 - 3, r / 7 - 3);
	}

	private int[] GenerateCoords(int count) {
		int[] result = Enumerable.Range(0, count).Select(_ => Random.Range(-3, 4)).ToArray();
		while (result.Sum() > 0) result[result.Where(s => s > -3).PickRandom()] -= 1;
		while (result.Sum() < 0) result[result.Where(s => s < 3).PickRandom()] += 1;
		return result;
	}
}
