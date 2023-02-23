# ffmpeg-qualityCompare

The workflow:
1) Have a ground truth source file (called REFERENCE in the code)
2) Create files to compare against (eg with different CRF or CQ or bitrate or bit depth or.. or..)
3) Run the program
4) Write the REFERENCE filename including extension (eg ref.mov)
5) Choose if all mov|mp4|mkv files should be compared
5.1) If yes (y) Move on
5.2) If no (n), write the compare filename, including the extension
6) Choose if the finished bat file should be executed.
7) Choose if the .txt resulting files should be averaged

