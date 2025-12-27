import json
import os

# Robust pathing relative to this script! 🗺️💎
BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA_DIR = os.path.join(BASE_DIR, "tests", "data")
OUTPUT_DIR = os.path.join(BASE_DIR, "output")

# Read the latest job results (from output folder)
job_result_path = os.path.join(OUTPUT_DIR, "transcript.json")
with open(job_result_path, "r", encoding="utf-8") as f:
    data = json.load(f)

# Create the speaker verification list
utterances = data.get("utterances", [])
verification_data = []
for u in utterances:
    verification_data.append(
        {"text": u["text"], "pyannote_assigned": u["speaker_id"], "label": ""}
    )

# Save to the new file in test data
output_path = os.path.join(DATA_DIR, "speaker_verification.json")
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(verification_data, f, indent=4)

print(f"✅ Created {output_path} with {len(verification_data)} entries! 🕵️‍♀️💎✨")
