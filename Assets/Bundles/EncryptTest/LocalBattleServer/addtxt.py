import os


def findAllFile(base):
    for root, ds, fs in os.walk(base):
        for f in fs:
            yield root, f

def changeName(path, file):
    if os.path.basename(file).endswith(".lua"):
        oldfilename = path + os.path.sep + file
        os.rename(oldfilename, oldfilename + ".txt")

def main():
    current_dir = os.getcwd()
    print(f"当前工作目录为：{current_dir}")
    for path, i in findAllFile('./battle/'):
        changeName(path, i)
    
    for path, i in findAllFile('./common/'):
        changeName(path, i)

    for path, i in findAllFile('./tables/'):
        changeName(path, i)

if __name__ == '__main__':
    main()