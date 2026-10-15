import os
import git

# נתיב לתיקיית הריפו המקומית
REPO_PATH = "."

# שם קובץ הפלט היחיד שבו ירוכז הכל
OUTPUT_FILE = "all_branches_combined.md"

# סיומות קבצים שנרצה לכלול
ALLOWED_EXTENSIONS = {
    ".py", ".cs", ".js", ".ts", ".json", ".md", ".txt", ".html", ".css", ".sql", ".yml", ".yaml"
}

# תיקיות להתעלם מהן
IGNORED_DIRS = {
    ".git", "__pycache__", "node_modules", "bin", "obj", "venv", ".vs", ".idea", "dist", "build"
}

def bundle_everything_into_one():
    try:
        repo = git.Repo(REPO_PATH)
    except Exception as e:
        print(f"שגיאה: התיקייה הנוכחית אינה ריפו פעיל של Git. פירוט: {e}")
        return

    # משיכת הנתונים המעודכנים ביותר מה-Remote (בלי לשנות קבצים אצלך בתיקייה)
    print("מבצע fetch לעדכון כל הענפים מה-Remote (GitHub)...")
    try:
        repo.remotes.origin.fetch()
        print("הנתונים עודכנו בהצלחה.\n")
    except Exception as e:
        print(f"אזהרה: לא ניתן לבצע fetch מול ה-Remote. ממשיך עם הנתונים הקיימים. פירוט: {e}")

    # איסוף ייחודי של כל הענפים (מקומיים + מרוחקים)
    branches_to_process = {}

    # 1. איסוף ענפים מקומיים
    for head in repo.heads:
        branches_to_process[head.name] = head.commit

    # 2. איסוף ענפים מרוחקים (של חברי צוות וכו')
    if repo.remotes:
        origin = repo.remotes.origin
        for ref in origin.refs:
            branch_name = ref.name
            if branch_name.endswith("/HEAD"):
                continue
            
            clean_name = branch_name.split("/", 1)[1] if "/" in branch_name else branch_name
            # נותן עדיפות או ממפה את הענף המרוחק תחת מפתח ברור
            branches_to_process[f"remote/{clean_name}"] = ref.commit

    print(f"נמצאו סך הכל {len(branches_to_process)} ענפים לשילוב לקובץ המרכזי.\n")

    total_files_processed = 0

    # פתיחת קובץ הפלט הגדול פעם אחת לכתיבה
    with open(OUTPUT_FILE, "w", encoding="utf-8") as outfile:
        outfile.write("# Complete Repository Snapshot (All Branches & Team Code)\n\n")
        outfile.write("This file contains the full source code across all local and remote branches.\n\n")

        for branch_key, commit in branches_to_process.items():
            print(f"מעבד ענף: {branch_key}...")
            
            # הוספת כותרת מפרידה לכל ענף בקובץ המאוחד
            outfile.write(f"\n\n==================================================\n")
            outfile.write(f"# BRANCH: {branch_key}\n")
            outfile.write(f"# Commit Hash: {commit.hexsha}\n")
            outfile.write(f"==================================================\n\n")
            
            try:
                tree = commit.tree
                count = process_tree_recursive(tree, "", outfile)
                total_files_processed += count
            except Exception as e:
                outfile.write(f"\n// Error processing branch {branch_key}: {e}\n")

    print(f"\n[סיום] כל הריפו רוכז בהצלחה לקובץ אחד ענק: {OUTPUT_FILE}")
    print(f"סך הכל הוכנסו מופעי קבצים (כולל כפילויות בין ענפים): {total_files_processed}")

def process_tree_recursive(tree, current_path, outfile):
    file_count = 0
    
    # מעבר על קבצים בעץ
    for blob in tree.blobs:
        file_name = blob.name
        file_ext = os.path.splitext(file_name)[1].lower()
        
        if file_ext in ALLOWED_EXTENSIONS:
            full_path = os.path.join(current_path, file_name)
            
            # בדיקת התעלמות מתיקיות מיותרות
            if any(ignored in full_path.split(os.sep) for ignored in IGNORED_DIRS):
                continue
                
            try:
                file_content = blob.data_stream.read().decode("utf-8", errors="ignore")
                
                outfile.write(f"\n--- FILE: {full_path} (Branch Path) ---\n")
                outfile.write(f"```{file_ext.lstrip('.')}\n")
                outfile.write(file_content)
                outfile.write("\n```\n")
                file_count += 1
            except Exception as e:
                outfile.write(f"\n--- FILE: {full_path} ---\n")
                outfile.write(f"// Error reading file content: {e}\n")

    # מעבר על תיקיות משנה בעץ
    for subtree in tree.trees:
        dir_name = subtree.name
        if dir_name in IGNORED_DIRS:
            continue
        new_path = os.path.join(current_path, dir_name) if current_path else dir_name
        file_count += process_tree_recursive(subtree, new_path, outfile)
        
    return file_count

if __name__ == "__main__":
    bundle_everything_into_one()