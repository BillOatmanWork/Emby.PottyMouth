define(["loading", "dialogHelper", "formDialogStyle", "emby-checkbox", "emby-select", "emby-toggle"],
    function(loading, dialogHelper) {

        var pluginId = "D9085D73-7142-4D82-905B-2A0B1949A6D2";

        return function(view) {
            view.addEventListener('viewshow',
                async () => {
                    var chkAutoMute = view.querySelector('#autoMute');

                    ApiClient.getPluginConfiguration(pluginId).then((config) => {
                        chkAutoMute.checked = config.EnablePottyMouth ?? false;
                    });

                    chkAutoMute.addEventListener('change', (elem) => {
                        elem.preventDefault();
                        var autoMute = chkAutoMute.checked;
                        enableAutoMute(autoMute);
                    });

                    function enableAutoMute(autoMute) {
                        ApiClient.getPluginConfiguration(pluginId).then((config) => {
                            config.EnablePottyMouth = autoMute;
                            ApiClient.updatePluginConfiguration(pluginId, config).then(() => {});
                        });
                    }
                });
        }
    });