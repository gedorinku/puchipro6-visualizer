/*
 *  -1�Ŏ���AI
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
    //��������U�Ȃ��0�C��U�Ȃ��1�����͂����D
    cin >> isSecond;
    //�A��̏������������̏������C�G�̏������̏��œ��͂����D
    cin >> won >> rivalWon;
    //AI�����o��
    cout << "InvalidAI" << endl;

    for (int k = 0; k < 10; ++k) {
        //���^�[���̓��͂́C�����̔Ֆʂ̏��C����̔Ֆʂ̏��̏��œ��͂����D

        int leftTime;
        //�c��̎v�l����(�~���b)�����͂����D
        cin >> leftTime;

        //�e��̃L���[�ɐς܂�Ă��邨����܂���́D
        for (int x = 1; x <= w; ++x) {
            ojama[x].clear();

            int count;
            //x��ڂ̃L���[�ɐς܂�Ă��邨����܂̐������
            cin >> count;

            for (int i = 0; i < count; ++i) {
                int b;
                //������܂����
                //-1 -> ���ʂ̂������
                //-2 -> �d���������
                cin >> b;
                ojama[x].push_back(b);
            }
        }
        for (int y = 1; y <= h; ++y) {
            for (int x = 1; x <= w; ++x) {
                cin >> field[x][y];
            }
        }

        //�������G�̏������
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

