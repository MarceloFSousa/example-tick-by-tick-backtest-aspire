namespace Domain.DLL.Services
{
    public static class StateService
    {
        public static string Verify(int nConnStateType, int result, ref bool connected, ref bool activated)
        {
            var resultText = "";

            switch (nConnStateType)
            {
                case 0: // login notifications
                    switch (result)
                    {
                        case 0:
                            resultText = "Login: Conectado";
                            break;
                        case 1:
                            resultText = "Login: Invalido";
                            break;
                        case 2:
                            resultText = "Login: Senha invalida";
                            break;
                        case 3:
                            resultText = "Login: Senha bloqueada";
                            break;
                        case 4:
                            resultText = "Login: Senha Expirada";
                            break;
                        case 200:
                            resultText = "Login: Erro Desconhecido";
                            break;
                    }
                    break;

                case 1: // broker notifications
                    switch (result)
                    {
                        case 0:
                            resultText = "Broker: Desconectado";
                            break;
                        case 1:
                            resultText = "Broker: Conectando";
                            break;
                        case 2:
                            resultText = "Broker: Conectado";
                            break;
                        case 3:
                            resultText = "Broker: HCS Desconectado";
                            break;
                        case 4:
                            resultText = "Broker: HCS Conectando";
                            break;
                        case 5:
                            resultText = "Broker: HCS Conectado";
                            break;
                    }
                    break;

                case 2: // Market login notifications
                    switch (result)
                    {
                        case 0:
                            resultText = "Market: Desconectado";
                            break;
                        case 1:
                            resultText = "Market: Conectando";
                            break;
                        case 2:
                            resultText = "Market: csConnectedWaiting";
                            break;
                        case 3:
                            connected = false;
                            resultText = "Market: Não logado";
                            break;
                        case 4:
                            connected = true;
                            resultText = "Market: Conectado";
                            break;
                    }
                    break;

                case 3: // Market login notifications
                    if (result == 0)
                    {
                        activated = true;
                        resultText = "Profit: Notificação de Atividade Valida";
                    }
                    else
                    {
                        activated = false;
                        resultText = "Profit: Notificação de Atividade Invalida";
                    }
                    break;
            }

            return resultText;
        }

    }
}
