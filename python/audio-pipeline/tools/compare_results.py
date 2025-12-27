import json
import os

# Robust pathing relative to this script! 🗺️💎
BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA_DIR = os.path.join(BASE_DIR, "tests", "data")
OUTPUT_DIR = os.path.join(BASE_DIR, "output")

# Read the latest job results (default output folder)
job_result_path = os.path.join(OUTPUT_DIR, "transcript.json")
with open(job_result_path, "r", encoding="utf-8") as f:
    job_data = json.load(f)

# Read the user's verification data
ver_data_path = os.path.join(DATA_DIR, "speaker_verification.json")
with open(ver_data_path, "r", encoding="utf-8") as f:
    ver_data = json.load(f)

print(
    f"{'Text (Snippet)':<40} | {'Pyannote New':<15} | {'Your Label':<15} | {'Match?'}"
)
print("-" * 90)

# The new structured output uses the utterances list directly
utterances = job_data.get("utterances", [])

for i, u in enumerate(utterances):
    text_snippet = u["text"][:37] + "..." if len(u["text"]) > 37 else u["text"]
    new_speaker = u["speaker_id"]
    your_label = ver_data[i]["label"] if i < len(ver_data) else "N/A"
    match = "✅" if new_speaker == your_label else "❌"

    print(f"{text_snippet:<40} | {new_speaker:<15} | {your_label:<15} | {match}")
