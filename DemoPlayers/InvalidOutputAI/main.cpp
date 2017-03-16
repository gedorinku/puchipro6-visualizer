/*
 *  -1で死ぬAI
 */

#include <iostream>
#include <vector>
#include <cstdlib>
#include <ctime>
using namespace std;

int field[500][500], rivalField[500][500], w, h, n, m, won, rivalWon;
vector<int> ojama[500], rivalOjama[500];

signed main() {
    ios::sync_with_stdio(false);
    cin.tie(0);

    srand((unsigned int)time(NULL));

    cin >> w >> h >> n >> m;
    bool isSecond;
    //自分が先攻ならば0，後攻ならば1が入力される．
    cin >> isSecond;
    //連戦の勝利数が自分の勝利数，敵の勝利数の順で入力される．
    cin >> won >> rivalWon;
    //AI名を出力
    cout << "InvalidAI" << endl;

    for (int k = 0; k < 10; ++k) {
        //毎ターンの入力は，自分の盤面の情報，相手の盤面の情報の順で入力される．

        int leftTime;
        //残りの思考時間(ミリ秒)が入力される．
        cin >> leftTime;

        //各列のキューに積まれているおじゃまを入力．
        for (int x = 1; x <= w; ++x) {
            ojama[x].clear();

            int count;
            //x列目のキューに積まれているおじゃまの数を入力
            cin >> count;

            for (int i = 0; i < count; ++i) {
                int b;
                //おじゃまを入力
                //-1 -> 普通のおじゃま
                //-2 -> 硬いおじゃま
                cin >> b;
                ojama[x].push_back(b);
            }
        }
        for (int y = 1; y <= h; ++y) {
            for (int x = 1; x <= w; ++x) {
                cin >> field[x][y];
            }
        }

        //同じく敵の情報を入力
        int rivalLefeTime;
        cin >> rivalLefeTime;

        for (int x = 1; x <= w; ++x) {
            rivalOjama[x].clear();

            int count;
            cin >> count;

            for (int i = 0; i < count; ++i) {
                int b;
                cin >> b;
                rivalOjama[x].push_back(b);
            }
        }
        for (int y = 1; y <= h; ++y) {
            for (int x = 1; x <= w; ++x) {
                cin >> rivalField[x][y];
            }
        }

        int nx = rand() % w + 1, ny = rand() % h + 1;
        cout << nx << " " << ny << endl;
    }

    return -1;
}

